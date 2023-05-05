using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualBasic.Devices;

namespace KeyboardIsMouse
{
    internal class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("KeyboardIsMouse 프로그램이 실행 중입니다. 종료하려면 Ctrl+C를 누르십시오.");

            // 사용자 입력을 처리하는 데 사용되는 키보드 후크 이벤트
            using (var keyboardHook = new KeyboardHook())
            {
                Application.Run();
            }
        }
    }

    public class KeyboardHook : IDisposable
    {
        private readonly IntPtr _hookID = IntPtr.Zero;
        public bool _winKeyDown;

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

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (key == Keys.LWin || key == Keys.RWin)
                    {
                        _winKeyDown = true;
                    }
                    else if (_winKeyDown)
                    {
                        if (ProcessKeyWithWin(key, true))
                        {
                            return (IntPtr)1;
                        }
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    if (key == Keys.LWin || key == Keys.RWin)
                    {
                        _winKeyDown = false;
                    }
                    else if (_winKeyDown)
                    {
                        if (ProcessKeyWithWin(key, false))
                        {
                            return (IntPtr)1;
                        }
                    }
                }

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP)
                {
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool ProcessKeyWithWin(Keys key, bool isKeyDown)
        {
            bool keyevent_pass = true;

            if (isKeyDown)
            {
                Debug.WriteLine("OnKeyDown");

                int moveAmount = 10; // 한 번에 이동할 픽셀 수를 설정합니다.
                GetCursorPos(out POINT currentPos);

                if (key == Keys.Home)
                {
                    Debug.WriteLine("HomeDown");
                    SetCursorPos(currentPos.X, currentPos.Y - moveAmount);
                }
                else if (key == Keys.End)
                {
                    Debug.WriteLine("EndDown");
                    SetCursorPos(currentPos.X, currentPos.Y + moveAmount);
                }
                else if (key == Keys.Delete)
                {
                    Debug.WriteLine("DeleteDown");
                    SetCursorPos(currentPos.X - moveAmount, currentPos.Y);
                }
                else if (key == Keys.PageDown)
                {
                    Debug.WriteLine("PageDownDown");
                    SetCursorPos(currentPos.X + moveAmount, currentPos.Y);
                }
                else
                {
                    keyevent_pass = false;
                }

                return keyevent_pass;
            }
            else
            {
                Debug.WriteLine("OnKeyUp");
                if (key == Keys.RShiftKey)
                {
                    Debug.WriteLine("RShiftKeyUP");
                    GetCursorPos(out POINT currentPos);
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                }
                else
                {
                    keyevent_pass = false;
                }

                return keyevent_pass;
            }
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