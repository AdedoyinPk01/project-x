using P21IntegrationWindowsService.Models;
using Sentry;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace P21IntegrationWindowsService
{
    public partial class P21IntegrationWindowsService : ServiceBase
    {
        public P21IntegrationWindowsService(string[] args)
        {
            InitializeComponent();
            string eventSourceName = "MySource";
            string logName = "MyNewLog";

            if (args.Length > 0) { eventSourceName = args[0]; }

            if (args.Length > 1) { logName = args[1]; }

            eventLog1 = new EventLog();

            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, logName);
            }

            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;
        }


        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Set up a timer that triggers every minute.
            Timer timer = new Timer();
            timer.Interval = 5 * 60 * 1000; // 300 seconds
            timer.Elapsed += new ElapsedEventHandler(Scheduler);
            timer.Start();
             
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry($"P21 Integration Service Stopped.");
        }

        public void Scheduler(object sender, ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.
            eventLog1.WriteEntry("Monitoring the Integration System", EventLogEntryType.Information);

            // Initialize Sentry to capture AppDomain unhandled exceptions and more.
            using (SentrySdk.Init(o =>
            {
                o.Dsn = ConfigurationManager.AppSettings["Dsn"];
                o.Environment = "Windows Service Logs";
                o.TracesSampleRate = 1.0;
            }))
            {
                SyncInitialization.StartSynchronization(eventLog1);
            }
        }

        public void OnDebug()
        {
            // Initialize Sentry to capture AppDomain unhandled exceptions and more.
            using (SentrySdk.Init(o =>
            {
                o.Dsn = "https://ac7905ef2bcf4bb8803dc679acfe56a9@o246824.ingest.sentry.io/6466655";
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;
                o.Environment = "Windows Service";
                // Set TracesSampleRate to 1.0 to capture 100%
                // of transactions for performance monitoring.
                // We recommend adjusting this value in production
                o.TracesSampleRate = 1.0;
            }))
            {
                //Set up a timer that triggers every minute.
                Timer timer = new Timer
                {
                    Interval = 2 * 60 * 1000 // x seconds
                };
                timer.Elapsed += new ElapsedEventHandler(Scheduler);
                timer.Start();
            }
        }

        // Set service status: report to Service Control Manager
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
