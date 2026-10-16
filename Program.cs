using System;
using System.Threading;
using System.Windows.Forms;

namespace WeekNumberLite
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (new Mutex(true, "88D1C6E0-96C8-4BB3-BA08-ACEFDC90A023", out bool firstInstance))
            {
                if (!firstInstance) return;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // There is no form, so install the context explicitly; SystemEvents
                // then raise their events on this UI thread.
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                using (TrayApplication application = new TrayApplication())
                    Application.Run(application);
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString(), "WeekNumber Lite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
}
