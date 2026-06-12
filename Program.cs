using System;
using System.IO;
using System.Windows.Forms;
using Utilities;

namespace SEBClone
{
    internal static class Program
    {
        /// <summary>
        /// Application entry point.
        /// Installs the low-level keyboard hook for kiosk mode, then launches
        /// the SplashScreen which automatically transitions to LoginForm after
        /// its timer expires.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Install the global keyboard hook that suppresses Alt+Tab,
                // Alt+F4, Ctrl+Esc, and the Windows keys.
                LockdownManager.InstallKeyboardHook();

                // Ensure the hook is removed cleanly when the application exits,
                // regardless of how it terminates.
                Application.ApplicationExit += (_, _) => LockdownManager.UninstallKeyboardHook();

                // Start with the splash screen; it will open LoginForm after 2.5 s.
                var splash = new Forms.SplashScreen();
                splash.Show();
                Application.Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), ex.ToString());
                MessageBox.Show(ex.ToString(), "Startup Error");
            }
        }
    }
}
