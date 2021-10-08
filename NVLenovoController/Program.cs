using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NVLenovoController
{
    static class Program
    {
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new main_app());
        }
        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = (Exception)e.ExceptionObject;

                LogException(ex);
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        private static void LogException(Exception ex)
        {
            if(!File.Exists("log.txt"))
            {
                File.Create("log.txt");
            }
            var logFile = "log.txt";

            using (FileStream stream = File.Open(logFile, FileMode.Append, FileAccess.Write,
                FileShare.ReadWrite))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("Date: " + DateTime.Now);
                    writer.WriteLine("Message: " + ex.Message);
                    writer.WriteLine("StackTrace: " + ex.StackTrace);
                }
            }
            Restart();
        }
        private static void Restart()
        {
            Process.Start(Application.ExecutablePath);
            Application.Exit();
        }
    }
}
