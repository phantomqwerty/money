using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Utilities
{
    /// <summary>
    /// Provides kiosk-mode lockdown functionality for Windows Forms applications.
    /// Call <see cref="ApplyLockdown"/> from a form's constructor or Load event,
    /// and <see cref="InstallKeyboardHook"/> once at application startup.
    /// Call <see cref="UninstallKeyboardHook"/> when the application exits.
    /// </summary>
    public static class LockdownManager
    {
        // ─────────────────────────────────────────────────────────────────────
        //  P/Invoke declarations
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Delegate type for a low-level keyboard hook procedure.
        /// </summary>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>Installs an application-defined hook procedure into a hook chain.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        /// <summary>Removes a hook procedure installed in a hook chain.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>Passes hook information to the next hook in the current hook chain.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        /// <summary>Retrieves a module handle for the specified module.</summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // ─────────────────────────────────────────────────────────────────────
        //  Win32 constants
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Hook type: low-level keyboard hook, receives all keystrokes.</summary>
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>Posted when a non-system key is pressed.</summary>
        private const int WM_KEYDOWN = 0x0100;

        /// <summary>Posted when a system key (e.g. Alt+X) is pressed.</summary>
        private const int WM_SYSKEYDOWN = 0x0104;

        /// <summary>Virtual-key code for the Left Windows key.</summary>
        private const int VK_LWIN = 0x5B;

        /// <summary>Virtual-key code for the Right Windows key.</summary>
        private const int VK_RWIN = 0x5C;

        /// <summary>Virtual-key code for the Tab key.</summary>
        private const int VK_TAB = 0x09;

        /// <summary>Virtual-key code for the F4 key.</summary>
        private const int VK_F4 = 0x73;

        /// <summary>Virtual-key code for the Escape key.</summary>
        private const int VK_ESCAPE = 0x1B;

        /// <summary>Window style flag — minimize box.</summary>
        private const int WS_MINIMIZEBOX = 0x20000;

        /// <summary>Window style flag — maximize box.</summary>
        private const int WS_MAXIMIZEBOX = 0x10000;

        // ─────────────────────────────────────────────────────────────────────
        //  Path to the unlock flag file
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Relative path of the file whose presence signals that lockdown is lifted.
        /// Resolved against <see cref="AppDomain.CurrentDomain.BaseDirectory"/> at runtime.
        /// </summary>
        private static readonly string UnlockFlagPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "unlock.flag");

        // ─────────────────────────────────────────────────────────────────────
        //  Hook state
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Handle to the installed keyboard hook; IntPtr.Zero when not installed.</summary>
        private static IntPtr _hookHandle = IntPtr.Zero;

        /// <summary>
        /// Keeps a managed reference to the hook delegate so the GC does not collect it
        /// while the unmanaged hook is still active.
        /// </summary>
        private static LowLevelKeyboardProc? _hookProc;

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures <paramref name="form"/> for full-screen kiosk mode.
        /// Call this inside the form's constructor (after <c>InitializeComponent()</c>)
        /// or in the <c>Load</c> event handler.
        /// </summary>
        /// <param name="form">The <see cref="Form"/> to lock down.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="form"/> is <c>null</c>.</exception>
        public static void ApplyLockdown(Form form)
        {
            if (form is null) throw new ArgumentNullException(nameof(form));

            // Remove all chrome so the user cannot drag, resize, or close the window.
            form.FormBorderStyle = FormBorderStyle.None;

            // Maximise the window so layout calculations use the full screen area.
            form.WindowState = FormWindowState.Maximized;

            // Keep the form above every other window, including pop-ups.
            form.TopMost = true;

            // Hide the system-menu icon (also prevents Alt+Space menu).
            form.ControlBox = false;

            // Manual start position so we can force exact screen bounds below.
            form.StartPosition = FormStartPosition.Manual;

            // Cover the entire primary screen including the taskbar area.
            form.Bounds = Screen.PrimaryScreen.Bounds;
        }

        /// <summary>
        /// Returns a modified <see cref="CreateParams"/> that strips the minimize and
        /// maximize boxes from the native window style.
        /// <para>
        /// Usage — override <c>CreateParams</c> in your form and delegate here:
        /// <code>
        /// protected override CreateParams CreateParams
        /// {
        ///     get => LockdownManager.GetLockedCreateParams(base.CreateParams);
        /// }
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="cp">The base <see cref="CreateParams"/> obtained from <c>base.CreateParams</c>.</param>
        /// <returns>A <see cref="CreateParams"/> with <c>WS_MINIMIZEBOX</c> and <c>WS_MAXIMIZEBOX</c> cleared.</returns>
        public static CreateParams GetLockedCreateParams(CreateParams cp)
        {
            cp.Style &= ~WS_MINIMIZEBOX; // Remove minimize button
            cp.Style &= ~WS_MAXIMIZEBOX; // Remove maximize button
            return cp;
        }

        /// <summary>
        /// Installs a system-wide low-level keyboard hook that blocks the following
        /// shortcuts from reaching the operating system:
        /// <list type="bullet">
        ///   <item><description>Alt+Tab</description></item>
        ///   <item><description>Alt+F4</description></item>
        ///   <item><description>Ctrl+Esc</description></item>
        ///   <item><description>Left/Right Windows key</description></item>
        /// </list>
        /// When <see cref="IsUnlocked"/> returns <c>true</c>, Alt+Tab and the Windows
        /// keys are allowed through so that normal task-switching remains available.
        /// </summary>
        /// <remarks>
        /// This method is idempotent — calling it a second time without first calling
        /// <see cref="UninstallKeyboardHook"/> is a no-op.
        /// </remarks>
        public static void InstallKeyboardHook()
        {
            if (_hookHandle != IntPtr.Zero)
                return; // Already installed.

            _hookProc = KeyboardHookCallback;

            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule? mainModule = currentProcess.MainModule)
            {
                string? moduleName = mainModule?.ModuleName;
                IntPtr moduleHandle = GetModuleHandle(moduleName!);
                _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, moduleHandle, 0);
            }

            if (_hookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"Failed to install keyboard hook. Win32 error code: {errorCode}");
            }
        }

        /// <summary>
        /// Removes the low-level keyboard hook installed by <see cref="InstallKeyboardHook"/>.
        /// Call this from your application's exit handler (e.g. <c>Application.ApplicationExit</c>
        /// or <c>FormClosed</c> on the main form).
        /// </summary>
        /// <remarks>
        /// This method is idempotent — calling it when no hook is installed is a no-op.
        /// </remarks>
        public static void UninstallKeyboardHook()
        {
            if (_hookHandle == IntPtr.Zero)
                return; // Nothing to uninstall.

            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
            _hookProc = null;
        }

        /// <summary>
        /// Checks whether the kiosk unlock flag file exists.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <c>Data/unlock.flag</c> exists relative to the application's
        /// base directory; <c>false</c> otherwise.
        /// </returns>
        public static bool IsUnlocked() => File.Exists(UnlockFlagPath);

        // ─────────────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Low-level keyboard hook procedure.  Intercepts key events before they are
        /// dispatched to the foreground application or the shell.
        /// </summary>
        /// <param name="nCode">
        /// Hook code.  When &lt; 0 the hook must call <see cref="CallNextHookEx"/> immediately.
        /// </param>
        /// <param name="wParam">Keyboard message identifier (e.g. WM_KEYDOWN).</param>
        /// <param name="lParam">Pointer to a <c>KBDLLHOOKSTRUCT</c> with key details.</param>
        /// <returns>
        /// A non-zero value to suppress the keystroke; otherwise the result of
        /// <see cref="CallNextHookEx"/>.
        /// </returns>
        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                // The first field of KBDLLHOOKSTRUCT is the virtual-key code (DWORD).
                int vkCode = Marshal.ReadInt32(lParam);

                bool altDown   = (Control.ModifierKeys & Keys.Alt)     != 0;
                bool ctrlDown  = (Control.ModifierKeys & Keys.Control) != 0;
                bool shiftDown = (Control.ModifierKeys & Keys.Shift) != 0;
                const int VK_G = 0x47;

                if (altDown && ctrlDown && shiftDown && vkCode == VK_G)
                {
                    Application.Exit();
                    return (IntPtr)1;
                }

                bool unlocked  = IsUnlocked();

                // ── Always-blocked combinations ──────────────────────────────
                // Alt+F4  — close active window
                if (altDown && vkCode == VK_F4)
                    return (IntPtr)1;

                // Ctrl+Esc — open Start menu
                if (ctrlDown && vkCode == VK_ESCAPE)
                    return (IntPtr)1;

                // ── Conditionally blocked (allowed when unlocked) ────────────
                if (!unlocked)
                {
                    // Alt+Tab — switch tasks
                    if (altDown && vkCode == VK_TAB)
                        return (IntPtr)1;

                    // Windows keys — open Start menu / task view
                    if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                        return (IntPtr)1;
                }
            }

            // Pass the keystroke to the next hook in the chain.
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
    }
}
