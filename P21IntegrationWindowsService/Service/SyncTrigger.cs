using Newtonsoft.Json;
using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static P21IntegrationWindowsService.Models.P21Objects;
using static P21IntegrationWindowsService.Models.ProkeepServiceModel;

namespace P21IntegrationWindowsService.Models
{
    public class SyncTrigger
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string p21EncodedString = ConfigurationManager.AppSettings["EncodedString"];
        private readonly string svcUsername = ConfigurationManager.AppSettings["svcUsername"];
        private readonly string svcPassword = ConfigurationManager.AppSettings["svcPassword"];
        private readonly EventLog _eventLog;
        private readonly ContactSyncContext context = new ContactSyncContext();
        private List<ContactSync> syncedContacts = new List<ContactSync>();

        public SyncTrigger(EventLog eventLog)
        {
            _eventLog = eventLog;
        }

        public async Task ProcessP21ContactSync(string prokeepSvcBaseUrl, string p21BaseUrl, string companyName, int icid)
        {
            try
            {
                string prokeepSvcUrl = $"{prokeepSvcBaseUrl}{companyName}/";
                // Get Contacts from P21
                string p21Url = $"{p21BaseUrl}entity/contacts/";
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", p21EncodedString);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                HttpResponseMessage p21response = await httpClient.GetAsync(p21Url);
                string stringContentXML = await p21response.Content.ReadAsStringAsync();
                ArrayOfContact p21Contacts = new ArrayOfContact();

                if (p21response.StatusCode == HttpStatusCode.OK)
                {
                    XDocument doc = XDocument.Parse(stringContentXML);
                    XmlSerializer xml = new XmlSerializer(typeof(ArrayOfContact));
                    p21Contacts = (ArrayOfContact)xml.Deserialize(doc.CreateReader());
                }

                // Get contacts from ContactSyncDB
                syncedContacts.Clear();
                syncedContacts = context.ContactSyncDB.ToList();
                _eventLog.WriteEntry($"P21 Contacts count: {p21Contacts.Contact.Length}, ContactSyncDB Count: {syncedContacts.Count}", EventLogEntryType.Information);

                // Compare Contacts from both results
                if (p21Contacts.Contact != null)
                {
                    foreach (P21Objects.Contact p21Contact in p21Contacts.Contact)
                    {
                        bool contains = false;
                        if (!string.IsNullOrWhiteSpace(p21Contact.DirectPhone) || !string.IsNullOrWhiteSpace(p21Contact.Cellular)) // Filter out P21 contacts without phonenumber
                        {
                            if (syncedContacts.Count > 0)
                            {
                                contains = syncedContacts.AsEnumerable()
                                                        .Any(row => p21Contact.Id == row.P21_ID && (p21Contact.Cellular.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) == row.phone_number
                                                                    || p21Contact.DirectPhone.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) == row.phone_number)
                                                        );
                            }

                            if (!contains) // New P21 Contact
                            {
                                _eventLog.WriteEntry($"Processing {p21Contact.Id} post to prokeep.", EventLogEntryType.Information);
                                HttpResponseMessage prokeepSvcResponse = await SendContactToProkeepSvc(p21Contact, httpClient, prokeepSvcUrl, icid);

                                if (prokeepSvcResponse?.StatusCode == HttpStatusCode.OK)
                                {
                                    try
                                    {
                                        // Send new contacts from Prokeep to ContactSyncDB
                                        var contactSaved = await AddContact(prokeepSvcResponse, "prokeepSvcResponse", p21Contact: p21Contact);

                                        var stringContent = await prokeepSvcResponse.Content.ReadAsStringAsync();
                                        var p21SyncedContact = JsonConvert.DeserializeObject<PSvcResponseObject>(stringContent);

                                        if (contactSaved)
                                        {
                                            string contactId = p21SyncedContact.contact?[0].pk_id;
                                            string p21ContactId = p21Contact.Id;
                                            await AcknowlegdeContactSync(contactId, p21ContactId, prokeepSvcBaseUrl, companyName);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        SentrySdk.CaptureException(e);
                                        _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                                        SentrySdk.CaptureMessage($"An error occured while adding P21 contact: {p21Contact.Id} to the ContactSyncDB.", level: SentryLevel.Error);
                                        _eventLog.WriteEntry($"An error occured while adding P21 contact: {p21Contact.Id} to the ContactSyncDB.", EventLogEntryType.Error);
                                    }
                                }
                                else
                                {
                                    SentrySdk.CaptureMessage($"P21 contact: {p21Contact.Id} request unsuccessful. Response Content: {prokeepSvcResponse?.Content.ReadAsStringAsync()}.", level: SentryLevel.Warning);
                                    _eventLog.WriteEntry($"P21 contact: {p21Contact.Id} request unsuccessful. Response Content: {prokeepSvcResponse?.Content.ReadAsStringAsync()}.", EventLogEntryType.Warning);
                                }
                            }
                            else
                            {
                                string phone = !string.IsNullOrEmpty(p21Contact.Cellular) ?
                                    p21Contact.Cellular.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) : !string.IsNullOrEmpty(p21Contact.DirectPhone) ?
                                    p21Contact.DirectPhone.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) : null;
                                string emailAddress = !string.IsNullOrEmpty(p21Contact.EmailAddress) ?
                                    p21Contact.EmailAddress.Trim().Replace("\t", "") : null;
                                string firstName = !string.IsNullOrEmpty(p21Contact.FirstName) ? p21Contact.FirstName.Trim().Replace("\t", "") : null;
                                string lastName = !string.IsNullOrEmpty(p21Contact.LastName) ? p21Contact.LastName.Trim().Replace("\t", "") : null;

                                try
                                {
                                    // Do a futher Check to see if it's updated or not
                                    ContactSync p21ContactToUpdate = context.ContactSyncDB.Where(contact => contact.P21_ID == p21Contact.Id && contact.phone_number == phone).FirstOrDefault();

                                    if (p21ContactToUpdate != null && (phone != p21ContactToUpdate.phone_number?.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10)
                                                          || emailAddress != p21ContactToUpdate.email_address?.Trim().Replace("\t", "")
                                                          || firstName != p21ContactToUpdate.firstname?.Trim().Replace("\t", "")
                                                          || lastName != p21ContactToUpdate.lastname?.Trim().Replace("\t", "")))
                                    {
                                        HttpResponseMessage prokeepSvcResponse = await SendContactToProkeepSvc(p21Contact, httpClient, prokeepSvcUrl, icid, contactId: p21ContactToUpdate.PK_ID);
                                        if (prokeepSvcResponse?.StatusCode == HttpStatusCode.OK)  // Update contactsyncdb
                                        {
                                            try
                                            {
                                                var contactSaved = await UpdateContact(p21ContactToUpdate.PK_ID, responseMessage: prokeepSvcResponse, responseType: "prokeepSvcResponse", p21Contact: p21Contact);

                                                var stringContent = await prokeepSvcResponse.Content.ReadAsStringAsync();
                                                var p21SyncedContact = JsonConvert.DeserializeObject<PSvcResponseObject>(stringContent);

                                                if (contactSaved)
                                                {
                                                    string contactId = p21SyncedContact.contact?[0].pk_id;
                                                    string p21ContactId = p21Contact.Id;
                                                    await AcknowlegdeContactSync(contactId, p21ContactId, prokeepSvcBaseUrl, companyName);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                SentrySdk.CaptureMessage($"An error occured while updating P21 contact with Prokeep Id: {p21ContactToUpdate.PK_ID} in the ContactSyncDB.", level: SentryLevel.Error);
                                                _eventLog.WriteEntry($"An error occured while updating P21 contact with Prokeep Id: {p21ContactToUpdate.PK_ID} in the ContactSyncDB.", EventLogEntryType.Error);
                                                SentrySdk.CaptureException(e);
                                                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                                            }
                                        }
                                        else
                                        {
                                            SentrySdk.CaptureMessage($"P21 contact: {p21Contact.Id} request unsuccessful. Response content: {prokeepSvcResponse?.Content.ReadAsStringAsync()}.", level: SentryLevel.Warning);
                                            _eventLog.WriteEntry($"P21 contact: {p21Contact.Id} request unsuccessful. Response content: {prokeepSvcResponse?.Content.ReadAsStringAsync()}.", EventLogEntryType.Warning);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    SentrySdk.CaptureMessage($"An error occured while updating P21 Contact: {p21Contact.Id}.", level: SentryLevel.Error);
                                    _eventLog.WriteEntry($"An error occured while updating P21 Contact: {p21Contact.Id}.", EventLogEntryType.Error);
                                    SentrySdk.CaptureException(e);
                                    _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                                    _ = e.Message;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                _ = e.Message;
            }
        }

        public async Task ProcessProkeepSvcContacts(string prokeepSvcBaseUrl, string companyName, string p21BaseUrl)
        {
            try
            {
                string prokeepSvcUrl = $"{prokeepSvcBaseUrl}{companyName}/";
                // Get contacts from ContactSyncDB
                syncedContacts.Clear();
                syncedContacts = context.ContactSyncDB.ToList();

                HttpResponseMessage prokeepSvcResponse = await GetContactsFromProkeepSvc(httpClient, prokeepSvcUrl);
                string stringContent = await prokeepSvcResponse.Content.ReadAsStringAsync();

                if (prokeepSvcResponse.StatusCode == HttpStatusCode.OK)
                {
                    ProkeepServiceModel.Rootobject prokeepContact = JsonConvert.DeserializeObject<ProkeepServiceModel.Rootobject>(stringContent);
                    bool contains = false;

                    foreach (ProkeepServiceModel.Contact pkContact in prokeepContact.contact)
                    {
                        if (!string.IsNullOrWhiteSpace(pkContact.phone_number)) // Filter out P21 contacts without phonenumber
                        {
                            _eventLog.WriteEntry($"Processing prokeep contact, ProkeepId: {pkContact.pk_id}", EventLogEntryType.Information);
                            if (syncedContacts.Count > 0)
                            {
                                contains = syncedContacts.AsEnumerable().Any(row => pkContact.external_id == row.P21_ID &&
                                                                                    pkContact.pk_id == row.PK_ID);
                            }

                            if (!contains) // Prokeep New Contacts
                            {
                                HttpResponseMessage p21ResponseMessage = await SendContactToP21(pkContact, httpClient, p21BaseUrl);

                                if (p21ResponseMessage.IsSuccessStatusCode)
                                {
                                    try
                                    {
                                        // Send new contacts from Prokeep to ContactSyncDB
                                        var contactSaved = await AddContact(p21ResponseMessage, "p21", pkContact: pkContact);

                                        if (contactSaved)
                                        {
                                            string contactId = pkContact.pk_id;
                                            var p21ContactId = DeserailizeP21Response(p21ResponseMessage).Result.Id;
                                            await AcknowlegdeContactSync(contactId, p21ContactId, prokeepSvcBaseUrl, companyName);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        SentrySdk.CaptureMessage($"An error occured while adding Prokeep contact: {pkContact.pk_id}.", level: SentryLevel.Error);
                                        _eventLog.WriteEntry($"An error occured while adding and acknowledging Prokeep contact: {pkContact.pk_id}.", EventLogEntryType.Error);
                                        SentrySdk.CaptureException(e);
                                        _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                                    }
                                }
                                else
                                {
                                    SentrySdk.CaptureMessage($"Prokeep contact: {pkContact.pk_id} request unsuccessful. Response content: {p21ResponseMessage?.Content.ReadAsStringAsync()}.", level: SentryLevel.Warning);
                                    _eventLog.WriteEntry($"Prokeep contact: {pkContact.pk_id} request unsuccessful. Response content: {p21ResponseMessage?.Content.ReadAsStringAsync()}.", EventLogEntryType.Warning);
                                }
                            }
                            else
                            {
                                string phone = !string.IsNullOrEmpty(pkContact.phone_number) ?
                                                    pkContact.phone_number.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) : null;
                                string emailAddress = !string.IsNullOrEmpty(pkContact.email_address?.ToString()) ? pkContact.email_address.ToString().Trim().Replace("\t", "") : null;
                                string firstName = !string.IsNullOrEmpty(pkContact.first_name?.ToString()) ? pkContact.first_name?.ToString().Trim().Replace("\t", "") : null;
                                string lastName = !string.IsNullOrEmpty(pkContact.last_name?.ToString()) ? pkContact.last_name?.ToString().Trim().Replace("\t", "") : null;

                                ContactSync contactToUpdate = context.ContactSyncDB.Where(contact => contact.PK_ID == pkContact.pk_id && contact.P21_ID == pkContact.external_id).FirstOrDefault();

                                // Do a futher Check to see if it's updated or not
                                if (phone != contactToUpdate.phone_number?.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10)
                                          || emailAddress != contactToUpdate.email_address?.Trim().Replace("\t", "")
                                          || firstName != contactToUpdate.firstname?.Trim().Replace("\t", "")
                                          || lastName != contactToUpdate.lastname?.Trim().Replace("\t", ""))
                                {
                                    HttpResponseMessage p21ResponseMessage = await SendContactToP21(pkContact, httpClient, contactToUpdate.PK_ID);
                                    if (p21ResponseMessage.IsSuccessStatusCode)
                                    {
                                        // Send new contacts from Prokeep to ContactSyncDB
                                        try
                                        {
                                            var contactSaved = await UpdateContact(ContactToUpdate: contactToUpdate.PK_ID, responseMessage: p21ResponseMessage, responseType: "p21");

                                            if (contactSaved)
                                            {
                                                string contactId = pkContact.pk_id;
                                                var p21ContactId = DeserailizeP21Response(p21ResponseMessage).Result.Id;
                                                await AcknowlegdeContactSync(contactId, p21ContactId, prokeepSvcBaseUrl, companyName);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            SentrySdk.CaptureMessage($"An error occured while adding Prokeep contact: {pkContact.pk_id} to contact sync db.", level: SentryLevel.Error);
                                            _eventLog.WriteEntry($"An error occured while adding Prokeep contact: {pkContact.pk_id} to contact sync db.", EventLogEntryType.Error);
                                            SentrySdk.CaptureException(e);
                                            _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                                        }
                                    }
                                    else
                                    {
                                        _ = SentrySdk.CaptureMessage($"Prokeep contact: {pkContact.pk_id} request unsuccessful. Response content: {p21ResponseMessage?.Content.ReadAsStringAsync()}.", level: SentryLevel.Warning);
                                        _eventLog.WriteEntry($"Prokeep contact: {pkContact.pk_id} request unsuccessful. Response content: {p21ResponseMessage?.Content.ReadAsStringAsync()}.", EventLogEntryType.Warning);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (prokeepSvcResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    _eventLog.WriteEntry($"No new contacts from prokeep", EventLogEntryType.Information);
                }
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            }
        }

        public async Task<HttpResponseMessage> SendRequest(HttpClient httpClient, object requestBody, string mediaType, string url, string authType = null, string encodedString = null, string contactId = null)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue($"application/{mediaType}"));

                if (!string.IsNullOrEmpty(authType))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"{authType} {encodedString}");
                }

                StringContent requestContent = new StringContent(requestBody.ToString(), Encoding.UTF8, $"application/{mediaType}");
                return contactId != null ? await httpClient.PutAsync(url, requestContent) : await httpClient.PostAsync(url, requestContent);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                return null;
            }
        }

        public async Task<HttpResponseMessage> SendContactToP21(ProkeepServiceModel.Contact contact, HttpClient httpClient, string p21BaseUrl, string contactId = null)
        {
            // Create and Address Id 
            string p21AddressId = CreateP21Address.CreateAddress();

            try
            {
                P21Objects.Contact p21RequestBody = new P21Objects.Contact()
                {
                    FirstName = contact.first_name?.ToString(),
                    LastName = contact.last_name?.ToString(),
                    AddressId = Convert.ToUInt32(p21AddressId),
                    Comments = "Prokeep Synced Contact",
                    DirectPhone = contact.phone_number?.ToString(),
                    Cellular = contact.phone_number?.ToString(),
                    EmailAddress = contact.email_address?.ToString(),
                };

                string requestContentXML = XmlSerialize(p21RequestBody);

                string url = $"{p21BaseUrl}entity/contacts/";

                // Post Record to P21
                HttpResponseMessage p21Response = await SendRequest(httpClient, requestBody: requestContentXML, authType: "Bearer", mediaType: "xml", encodedString: p21EncodedString, url: url, contactId: contactId);
                return p21Response;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureMessage($"An error occured while proceesing the request to send Prokeep contact: {contact.pk_id} to P21.", SentryLevel.Error);
                _eventLog.WriteEntry($"An error occured while proceesing the request to send Prokeep contact: {contact.pk_id} to P21.", EventLogEntryType.Error);
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                return null;
            }
        }

        public async Task<HttpResponseMessage> SendContactToProkeepSvc(P21Objects.Contact contact, HttpClient httpClient, string prokeepSvcUrl, int icid, string contactId = null, string requestType = null)
        {
            try
            {
                ProkeepServiceModel.Rootobject prokeepSvc = new ProkeepServiceModel.Rootobject() { };
                prokeepSvc.customer_name = context.IntSystemSettings.Where(setting => setting.ICID == icid).FirstOrDefault().Company;
                prokeepSvc.contact = new ProkeepServiceModel.Contact[1];
                ProkeepServiceModel.Contact prokeepSvcContact = new ProkeepServiceModel.Contact
                {
                    type = !string.IsNullOrEmpty(contactId) ? "update" : "new",
                    customer_id = null,
                    email_address = contact.EmailAddress.Trim().Replace("\t", ""),
                    external_id = contact.Id,
                    first_name = contact.FirstName.Trim(),
                    last_name = contact.LastName.Trim(),
                    phone_number = !string.IsNullOrEmpty(contact.Cellular) ?
                        contact.Cellular.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) :
                        contact.DirectPhone.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10),
                    pk_id = contactId,
                    fax_number = null,
                };

                prokeepSvc.contact[0] = prokeepSvcContact;

                string jsonRequestBody = JsonConvert.SerializeObject(prokeepSvc);
                string svcEncodedString = EncodeUsrPwd(svcUsername, svcPassword);

                // Post Record 
                HttpResponseMessage response = await SendRequest(httpClient, requestBody: jsonRequestBody, mediaType: "json", url: prokeepSvcUrl, authType: "Basic", encodedString: svcEncodedString);
                return response;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureMessage($"An error occured while processing P21 Contact: {contact.Id} post Prokeep svc", SentryLevel.Error);
                _eventLog.WriteEntry($"An error occured while processing P21 Contact: {contact.Id} post Prokeep svc", EventLogEntryType.Error);
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                return null;
            }
        }

        public async Task<bool> AddContact(HttpResponseMessage responseMessage, string responseType, ProkeepServiceModel.Contact pkContact = null, P21Objects.Contact p21Contact = null)
        {
            PSvcResponseObject prokeepResponseObject = new PSvcResponseObject();

            if (responseType == "p21")
            {
                p21Contact = await DeserailizeP21Response(responseMessage);
            }
            else
            {
                var stringContent = await responseMessage.Content.ReadAsStringAsync();
                prokeepResponseObject = JsonConvert.DeserializeObject<PSvcResponseObject>(stringContent);
            }

            try
            {
                // Update the ContactSync DB
                ContactSync sendContact = new ContactSync
                {
                    P21_ID = p21Contact.Id,
                    PK_ID = !string.IsNullOrEmpty(prokeepResponseObject.contact?[0].pk_id) ? prokeepResponseObject.contact?[0].pk_id : !string.IsNullOrEmpty(pkContact.pk_id) ? pkContact.pk_id : null,
                    firstname = !string.IsNullOrEmpty(p21Contact.FirstName) ? p21Contact.FirstName.Trim() : null,
                    lastname = !string.IsNullOrEmpty(p21Contact.LastName) ? p21Contact.LastName.Trim() : null,
                    email_address = !string.IsNullOrEmpty(p21Contact.EmailAddress) ? p21Contact.EmailAddress.Trim().Replace("\t", "") : null,
                    phone_number = !string.IsNullOrEmpty(p21Contact.Cellular) ?
                        p21Contact.Cellular.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) :
                        !string.IsNullOrEmpty(p21Contact.DirectPhone) ?
                        p21Contact.DirectPhone.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) : null,
                    status = 1,
                    synced_on = DateTime.Now,
                    sync_trigger_by = "P21 Integration",
                    update_trigger_by = !string.IsNullOrEmpty(prokeepResponseObject.contact?[0].updated_by_user_id) ? prokeepResponseObject.contact?[0].updated_by_user_id : null,
                    updated_on = DateTime.Now
                };

                _ = context.ContactSyncDB.Add(sendContact);
                _ = await context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                _eventLog.WriteEntry($"An error occurred while saving contact: P21 Id: {p21Contact?.Id}");
                return false;
            }
        }

        public async Task<bool> UpdateContact(string ContactToUpdate, string responseType, HttpResponseMessage responseMessage, P21Objects.Contact p21Contact = null)
        {
            PSvcResponseObject p21responseObject = new PSvcResponseObject();
            if (responseType == "p21")
            {
                p21Contact = await DeserailizeP21Response(responseMessage);
            }
            else
            {
                string stringContent = await responseMessage.Content.ReadAsStringAsync();
                p21responseObject = JsonConvert.DeserializeObject<PSvcResponseObject>(stringContent);
            }

            try
            {
                ContactSync data = context.ContactSyncDB.Where(contact => contact.PK_ID == ContactToUpdate).FirstOrDefault();
                if (responseType == "prokeepSvcResponse")
                {
                    data.sync_trigger_by = p21responseObject.contact?[0].inserted_by_user_id;
                    data.update_trigger_by = p21responseObject.contact?[0].updated_by_user_id;
                }
                else
                {
                    data.PK_ID = ContactToUpdate;
                }

                data.P21_ID = p21Contact.Id;
                data.firstname = !string.IsNullOrEmpty(p21Contact.FirstName) ? p21Contact.FirstName.Trim() : null;
                data.lastname = !string.IsNullOrEmpty(p21Contact.LastName) ? p21Contact.LastName.Trim() : null;
                data.email_address = !string.IsNullOrEmpty(p21Contact.EmailAddress) ? p21Contact.EmailAddress.Trim().Replace("\t", "") : null;
                data.phone_number = !string.IsNullOrEmpty(p21Contact.Cellular) ?
                        p21Contact.Cellular.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) :
                        !string.IsNullOrEmpty(p21Contact.DirectPhone) ?
                        p21Contact.DirectPhone.Trim().Replace("\t", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace(" ", "").Replace("+", "").Replace("x", "").Substring(0, 10) : null;
                data.status = 1;
                data.sync_trigger_by = data.sync_trigger_by;
                data.synced_on = data.synced_on;
                data.updated_on = DateTime.Now;
                _ = await context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureMessage($"Failed to add contact: {p21Contact.Id} in ContactSyncDB.", level: SentryLevel.Error);
                SentrySdk.CaptureException(e);
                _eventLog.WriteEntry($"Failed to add contact: {p21Contact.Id} in ContactSyncDB.", EventLogEntryType.Error);
                _eventLog.WriteEntry(e.Message, EventLogEntryType.Error);

                return false;
            }
        }

        public async Task<HttpResponseMessage> GetContactsFromProkeepSvc(HttpClient httpClient, string prokeepContactUrl)
        {
            string prokeepEncodedstring = EncodeUsrPwd(svcUsername, svcPassword);

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {prokeepEncodedstring}");
            HttpResponseMessage pkSvcResponse = await httpClient.GetAsync(prokeepContactUrl);

            return pkSvcResponse;
        }

        public async Task AcknowlegdeContactSync(string Id, string p21Id, string prokeepSvcBaseUrl, string companyName)
        {
            AckObject ackObject = new AckObject { contact = new AckBody[1] };
            AckBody ackBody = new AckBody
            {
                pk_id = Id,
                external_id = p21Id,
                status = "OK",
            };
            ackObject.contact[0] = ackBody;

            string requestBody = JsonConvert.SerializeObject(ackObject);
            string ackUrl = $"{prokeepSvcBaseUrl}Ack/{companyName}/";

            HttpResponseMessage responseMessage = await SendRequest(httpClient, requestBody, "json", ackUrl, "Basic", EncodeUsrPwd(svcUsername, svcPassword));

            if (responseMessage?.StatusCode == HttpStatusCode.OK)
            {
                _eventLog.WriteEntry($"{Id} Sync Completed");
            }
            else
            {
                _eventLog.WriteEntry($"An error occurred while syncing {Id}");
            }
        }

        public string EncodeUsrPwd(string username, string password)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            return Convert.ToBase64String(byteArray);
        }

        public async Task<P21Objects.Contact> DeserailizeP21Response(HttpResponseMessage responseMessage)
        {
            XDocument doc = XDocument.Parse(await responseMessage.Content.ReadAsStringAsync());
            XmlSerializer xml = new XmlSerializer(typeof(P21Objects.Contact));
            var p21Contact = (P21Objects.Contact)xml.Deserialize(doc.CreateReader());
            return p21Contact;
        }

        public static string XmlSerialize<T>(T entity) where T : class
        {
            // removes version
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            };

            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
            using (StringWriter sw = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                // removes namespace
                XmlSerializerNamespaces xmlns = new XmlSerializerNamespaces();
                xmlns.Add(string.Empty, string.Empty);

                xsSubmit.Serialize(writer, entity, xmlns);
                return sw.ToString(); // Your XML
            }
        }
    }
}
