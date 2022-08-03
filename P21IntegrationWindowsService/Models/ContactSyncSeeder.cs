using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService.Models
{
    public class ContactSyncSeeder
    {
        private readonly static string p21BaseUrl = ConfigurationManager.AppSettings["URL"];
        private readonly static string svcBaseUrl = ConfigurationManager.AppSettings["svcUrl"];

        public static void Seed(ContactSyncContext context)
        {
            var date = DateTime.Now;
            var frequency = 24 * 60;

            context.IntSystemSettings.Add(new Settings()
            {
                Frequency = frequency,
                SvcUrl = svcBaseUrl,
                P21Url = p21BaseUrl,
                Company = "LiveOak",
                LastRun = date,
                NextRun = date.AddMinutes(frequency)
            });

            context.SaveChanges();
        }
    }
}
