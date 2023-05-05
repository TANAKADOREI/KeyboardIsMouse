using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualBasic.Devices;

namespace KeyboardIsMouse
{
    internal class Program
    {
        static NotifyIcon trayIcon;

        static void Main(string[] args)
        {
            using (Mutex mutex = new Mutex(false, "KeyboardIsMouse"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("Already Exists", "KeyboardIsMouse", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                using (var keyboardHook = new KeyboardHook())
                {
                    trayIcon = new NotifyIcon
                    {
                        Icon = SystemIcons.Application,
                        Text = "KeyboardIsMouse",
                        Visible = true
                    };

                    trayIcon.Click += (sender, args) => Application.Exit();

                    Application.Run();
                }
            }
        }
    }

    public class KeyboardHook : IDisposable
    {
        private readonly IntPtr _hookID = IntPtr.Zero;
        public bool _winKeyDown = false;


        public KeyboardHook()
        {
            _hookID = SetHook(HookCallback);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (!_winKeyDown && wParam == (IntPtr)WM_KEYDOWN && key == Keys.RWin)
                {
                    _winKeyDown = true;
                }
                else if (_winKeyDown && wParam == (IntPtr)WM_KEYUP && key == Keys.RWin)
                {
                    _winKeyDown = false;
                }

                else if (wParam != (IntPtr)WM_KEYDOWN)
                {
                    goto pass;
                }

                if (_winKeyDown)
                {

                    bool pass_key = true;
                    int moveAmount = 10;
                    GetCursorPos(out POINT currentPos);

                    if (key == Keys.Home)
                    {
                        SetCursorPos(currentPos.X, currentPos.Y - moveAmount);
                    }
                    else if (key == Keys.End)
                    {
                        SetCursorPos(currentPos.X, currentPos.Y + moveAmount);
                    }
                    else if (key == Keys.Delete)
                    {
                        SetCursorPos(currentPos.X - moveAmount, currentPos.Y);
                    }
                    else if (key == Keys.PageDown)
                    {
                        SetCursorPos(currentPos.X + moveAmount, currentPos.Y);
                    }
                    else if (key == Keys.RShiftKey)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                        mouse_event(MOUSEEVENTF_LEFTUP, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                    }
                    else if (key == Keys.RControlKey || key == Keys.HanjaMode)
                    {
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                        mouse_event(MOUSEEVENTF_RIGHTUP, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                    }
                    else
                    {
                        pass_key = false;
                    }

                    if (pass_key)
                    {
                        return (IntPtr)1;
                    }
                }

                if (key == Keys.RWin)
                {
                    return (IntPtr)1;
                }
            }

        pass:
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        ~KeyboardHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }
        }

        public struct POINT
        {
            public int X;
            public int Y;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;


        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}