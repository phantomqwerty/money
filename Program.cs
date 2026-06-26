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

                // Create desktop shortcut if it doesn't exist
                try
                {
                    string shortcutPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "SEBClone.lnk");
                    if (!File.Exists(shortcutPath))
                    {
                        string exePath = Application.ExecutablePath;
                        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            "Assets", "icons", "Application.ico");
                        // Use WScript.Shell COM object to create shortcut
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell")!;
                        dynamic shell = Activator.CreateInstance(shellType)!;
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = exePath;
                        shortcut.IconLocation = iconPath;
                        shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        shortcut.Description = "SEBClone Exam Browser";
                        shortcut.Save();
                    }
                }
                catch { /* silently skip if shortcut creation fails */ }

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
