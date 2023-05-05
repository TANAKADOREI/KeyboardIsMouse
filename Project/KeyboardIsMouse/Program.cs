using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualBasic.Devices;
using System.Text;
using System.Timers;

namespace KeyboardIsMouse
{
    
    class Program
    {
        class KeyboardHook : IDisposable
        {
            enum DirFlags
            {
                NONE = 0,
                LEFT = 1,
                RIGHT = 2,
                UP = 4,
                DOWN = 8,
            }

            const float AMOUNT = 0.4f;
            const int FPS = 144;

            readonly IntPtr m_hook_handle = IntPtr.Zero;

            bool m_key_down = false;
            bool m_hook_proc_run = false;
            DirFlags m_direction_flags = DirFlags.NONE;
            float m_cursor_move_dir_x = 0;
            float m_cursor_move_dir_y = 0;

            private void Loop(object? sender, ElapsedEventArgs e)
            {
                if (!m_hook_proc_run)
                {
                    m_direction_flags = DirFlags.NONE;
                }

                if (m_direction_flags == DirFlags.NONE)
                {
                    m_cursor_move_dir_x = m_cursor_move_dir_y = 0;
                }
                else
                {
                    if (m_direction_flags.HasFlag(DirFlags.UP)) m_cursor_move_dir_y -= AMOUNT;
                    if (m_direction_flags.HasFlag(DirFlags.DOWN)) m_cursor_move_dir_y += AMOUNT;
                    if (m_direction_flags.HasFlag(DirFlags.RIGHT)) m_cursor_move_dir_x += AMOUNT;
                    if (m_direction_flags.HasFlag(DirFlags.LEFT)) m_cursor_move_dir_x -= AMOUNT;
                }

                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("State : |");
                    if (m_direction_flags.HasFlag(DirFlags.UP)) builder.Append("UP|");
                    if (m_direction_flags.HasFlag(DirFlags.DOWN)) builder.Append("DOWN|");
                    if (m_direction_flags.HasFlag(DirFlags.RIGHT)) builder.Append("RIGHT|");
                    if (m_direction_flags.HasFlag(DirFlags.LEFT)) builder.Append("LEFT|");
                    Debug.WriteLine(builder.ToString());
                }

                if (m_direction_flags != DirFlags.NONE)
                {
                    GetCursorPos(out POINT cur_point);
                    cur_point.X += (int)m_cursor_move_dir_x;
                    cur_point.Y += (int)m_cursor_move_dir_y;
                    SetCursorPos(cur_point.X, cur_point.Y);
                }
            }

            public KeyboardHook()
            {
                var timer = new System.Timers.Timer(1000 / FPS);
                timer.Elapsed += Loop;
                timer.Start();
                m_hook_handle = SetHook(HookCallback);
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
                    bool pass_key = true;

                    if (!m_hook_proc_run && key == Keys.HanjaMode && wParam == (IntPtr)WM_KEYDOWN)
                    {
                        m_hook_proc_run = true;
                    }
                    else if (m_hook_proc_run && key == Keys.HanjaMode && wParam == (IntPtr)WM_KEYUP)
                    {
                        m_hook_proc_run = false;
                    }

                    if (wParam == (IntPtr)WM_KEYDOWN)
                    {
                        m_key_down = true;
                    }
                    else if (wParam == (IntPtr)WM_KEYUP)
                    {
                        m_key_down = false;
                    }

                    if (!m_hook_proc_run) goto pass;

                    //Debug.WriteLine(Enum.GetName(key));

                    if (key == Keys.Home)
                    {
                        if (m_key_down)
                        {
                            m_direction_flags |= DirFlags.UP;
                        }
                        else
                        {
                            m_direction_flags &= ~DirFlags.UP;
                        }
                    }
                    else if (key == Keys.End)
                    {
                        if (m_key_down)
                        {
                            m_direction_flags |= DirFlags.DOWN;
                        }
                        else
                        {
                            m_direction_flags &= ~DirFlags.DOWN;
                        }
                    }
                    else if (key == Keys.Delete)
                    {
                        if (m_key_down)
                        {
                            m_direction_flags |= DirFlags.LEFT;
                        }
                        else
                        {
                            m_direction_flags &= ~DirFlags.LEFT;
                        }
                    }
                    else if (key == Keys.PageDown)
                    {
                        if (m_key_down)
                        {
                            m_direction_flags |= DirFlags.RIGHT;
                        }
                        else
                        {
                            m_direction_flags &= ~DirFlags.RIGHT;
                        }
                    }
                    else if (key == Keys.Insert)
                    {
                        GetCursorPos(out POINT currentPos);
                        if (m_key_down)
                        {
                            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                        }
                        else
                        {
                            mouse_event(MOUSEEVENTF_LEFTUP, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                        }
                    }
                    else if (key == Keys.PageUp)
                    {
                        GetCursorPos(out POINT currentPos);
                        if (m_key_down)
                        {
                            mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                        }
                        else
                        {
                            mouse_event(MOUSEEVENTF_RIGHTUP, (uint)currentPos.X, (uint)currentPos.Y, 0, 0);
                        }
                    }
                    else
                    {
                        pass_key = false;
                    }

                    if (pass_key)
                    {
                        return (IntPtr)1;
                    }

                    if (key == Keys.HanjaMode)
                    {
                        return (IntPtr)1;
                    }
                }

            pass:
                return CallNextHookEx(m_hook_handle, nCode, wParam, lParam);
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
                if (m_hook_handle != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(m_hook_handle);
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

        static void Main(string[] args)
        {
            NotifyIcon trayIcon;

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
}