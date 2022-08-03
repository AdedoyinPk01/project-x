using P21.DomainObject.Entity.Customer;
using P21.DomainObject.Security;
using P21.Soa.Client.Rest;
using P21.Soa.Client.Rest.Entity;
using P21.Soa.Client.Rest.Security;
using P21.Soa.Client.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService.Models
{
    class CreateP21Address
    {
        private static readonly string p21BaseUrl = ConfigurationManager.AppSettings["URL"];

        public static string CreateAddress()
        {
            try
            {
                // Get a valid token using P21 user credentials
                // A valid URL will look similar to https://ABCBuildersInc.net:3448/api
                Token token = TokenResourceClient.AuthenticateUser(p21BaseUrl, "admin", "Live0@kP21!@#");

                // Instantiate an entity resource client
                RestClientSecurity rcs = RestResourceClientHelper.GetClientSecurity(token);
                AddressResourceClient addressClient = new AddressResourceClient(p21BaseUrl, rcs);

                // Instantiate and populate the entity
                Address address = new Address();

                address.Name = "ABC Builders Inc.";
                address.MailAddress1 = "100 Main Street";
                address.MailCity = "Philadelphia";
                address.MailState = "PA";
                address.MailPostalCode = "19124";
                address.MailCountry = "USA";
                address.PhysAddress1 = address.MailAddress1;
                address.PhysCity = address.MailCity;
                address.PhysState = address.MailState;
                address.PhysPostalCode = address.MailPostalCode;
                address.PhysCountry = address.MailCountry;
                address.CentralPhoneNumber = "215-555-5555";

                // Insert the new address
                Address insertedAddress = addressClient.Resource.CreateAddress(address);
                Console.WriteLine("Address ID inserted: " + insertedAddress.AddressId.ToString());

                // Retrieve the address created previously
                Address retrievedAddress = addressClient.Resource.GetAddress(insertedAddress.AddressId.ToString());
                Console.WriteLine("Address ID retrieved: " + retrievedAddress.AddressId.ToString());

                // Set values for a few additional fields on the retrieved address
                retrievedAddress.MailAddress2 = "Suite 242";
                retrievedAddress.PhysAddress2 = retrievedAddress.MailAddress2;

                // Update the address
                Address updatedAddress = addressClient.Resource.CreateAddress(retrievedAddress);
                return updatedAddress.AddressId.ToString();
            }
            catch (Exception ex)
            {
                return "Exception encountered: " + ex.Message;
            }
        }
    }
}
