using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Permissions;

namespace STREDIT
{
    static class Program
    {
        public static string DATAFOLDER = Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\CraftNet\\STREDIT\\";


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            DLOG.WriteLine("STREDIT starting. Set UnhandledExceptionFilter!");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);

            string SourceName = "WindowsService.ExceptionLog";
            if (!EventLog.SourceExists(SourceName))
            {
                EventLog.CreateEventSource(SourceName, "STREDIT");
            }

            EventLog eventLog = new EventLog();
            eventLog.Source = SourceName;
            string message = string.Format("Exception: {0} \n\nStack: {1}", e.Message, e.ToString());
            eventLog.WriteEntry(message, EventLogEntryType.Error);
            System.IO.File.WriteAllText(DATAFOLDER + "STREDIT_ERROR.txt", e.ToString());
            System.Windows.Forms.MessageBox.Show("Exception occurred. Look inside the event viewer to get the error.");
        }

    }
}
