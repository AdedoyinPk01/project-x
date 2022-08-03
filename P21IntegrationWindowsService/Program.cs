using Microsoft.EntityFrameworkCore;
using P21IntegrationWindowsService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (var context = new ContactSyncContext())
            {
                context.Database.Migrate();
            }

#if DEBUG
            
            P21IntegrationWindowsService service = new P21IntegrationWindowsService(args);
            service.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new P21IntegrationWindowsService(args)
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
