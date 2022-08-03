using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService.Models
{
    public static class SyncInitialization
    {
        public static async void StartSynchronization(EventLog eventLog) //event 
        {
            try
            {
                ContactSyncContext context = new ContactSyncContext();
                List<Settings> settings = new List<Settings>();
                try
                {
                    eventLog.WriteEntry("Setting up integration config.", EventLogEntryType.Information);
                    settings = await context.IntSystemSettings.ToListAsync();
                }
                catch (SqliteException e)
                {
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Information);
                    ContactSyncSeeder.Seed(context);
                    settings = await context.IntSystemSettings.ToListAsync();
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Information);
                }

                if (settings.Count == 0)
                {
                    eventLog.WriteEntry("Integration settings hasn't been configured.", EventLogEntryType.Information);
                    eventLog.WriteEntry("Configuring Integration Settings.", EventLogEntryType.Information);
                    ContactSyncSeeder.Seed(context);
                    eventLog.WriteEntry("Integration Settings Configured successfully.", EventLogEntryType.Information);
                }
                else
                {
                    string nextRun = settings[0].NextRun.ToString();
                    string lastRun = settings[0].LastRun.ToString();
                    double frequency = double.Parse(settings[0].Frequency.ToString());

                    DateTime next_run = (!string.IsNullOrEmpty(nextRun)) ? DateTime.Parse(nextRun)
                            : DateTime.Parse("1900/01/01");
                    DateTime last_run = (!string.IsNullOrEmpty(lastRun)) ? DateTime.Parse(lastRun)
                        : DateTime.Parse("1900/01/01");

                    if (next_run == DateTime.Parse("1900/01/01") || last_run == DateTime.Parse("1900/01/01"))
                    {
                        eventLog.WriteEntry("Contact Synchrozation started.", EventLogEntryType.Information);
                        await InitiateContactSync(context, frequency, eventLog);
                        eventLog.WriteEntry("Contact Synchrozation ended.", EventLogEntryType.Information);
                    }
                    else if (next_run <= DateTime.Now)
                    {
                        //Subtract last run from current time to determine if notification should be sent
                        double minutes = next_run.Subtract(last_run).TotalMinutes;
                        if (minutes >= frequency)
                        {
                            eventLog.WriteEntry("Contact Synchrozation started.", EventLogEntryType.Information);
                            await InitiateContactSync(context, frequency, eventLog);
                            eventLog.WriteEntry("Contact Synchrozation ended.", EventLogEntryType.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }

        private static async Task InitiateContactSync(ContactSyncContext context, double frequency, EventLog eventLog)
        {
            int icid = Convert.ToInt32(ConfigurationManager.AppSettings["ICID"]);
            Settings integrationSettings = context.IntSystemSettings.Where(setting => setting.ICID == icid).FirstOrDefault();
            var date = DateTime.Now;
            integrationSettings.NextRun = date.AddMinutes(frequency);
            await context.SaveChangesAsync();

            // Send Request and process the contacts
            SyncTrigger syncTrigger = new SyncTrigger(eventLog);
            await syncTrigger.ProcessProkeepSvcContacts(integrationSettings.SvcUrl, integrationSettings.Company.ToLower(), integrationSettings.P21Url);
            await syncTrigger.ProcessP21ContactSync(integrationSettings.SvcUrl, integrationSettings.P21Url, integrationSettings.Company.ToLower(), icid);

            // Set next run and last run columns in the settings db
            integrationSettings.LastRun = DateTime.Now;
            integrationSettings.NextRun = integrationSettings.LastRun?.AddMinutes(frequency);
            await context.SaveChangesAsync();
        }
    }
}