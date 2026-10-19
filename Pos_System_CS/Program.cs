using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pos_System_CS
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Global exception handlers
            Application.ThreadException += (sender, args) =>
            {
                try
                {
                    Logger.LogError("Unhandled UI thread exception", args.Exception);
                }
                catch { }
                MessageBox.Show("An unexpected error occurred. The application will try to continue. See log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                try
                {
                    var ex = args.ExceptionObject as Exception;
                    Logger.LogError("Unhandled domain exception", ex);
                }
                catch { }
                try
                {
                    MessageBox.Show("A fatal error occurred. The application will close. See log for details.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
