using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace dyned_autoclicker
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        //模拟鼠标滚轮滚动操作，必须配合dwData参数
        const int MOUSEEVENTF_WHEEL = 0x0800;

        [DllImport("user32")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        public struct KeyMSG
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        // 安装钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        // 卸载钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);
        // 继续下一个钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);
        // 取得当前线程编号
        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        static extern uint SetThreadExecutionState(ExecutionFlag flags);


        [Flags]
        enum ExecutionFlag : uint
        {
            System = 0x00000001,
            Display = 0x00000002,
            Continus = 0x80000000,
        }

        public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);
        static int hKeyboardHook = 0;//如果hKeyboardHook不为0则说明钩子安装成功
        HookProc KeyboardHookProcedure;
        static int hMouseHook = 0;
        HookProc mouseHookProcedure;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            button1.Enabled = false;
        }

        public Thread thread_click;
        public bool thread_stop_attitude = false;

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(delegate ()
            {
                label1.Text = "将在3秒后开始！";
                Thread.Sleep(1000);

                label1.Text = "将在2秒后开始！";
                Thread.Sleep(1000);

                label1.Text = "将在1秒后开始！";
                Thread.Sleep(1000);

                label1.Text = "可以随时按空格结束！";
                button1.Enabled = false;
                button2.Enabled = false;
            });
            t.Start();
            t.Join();
            Start_Click();
            thread_stop_attitude = false;
            thread_click = new System.Threading.Thread(new System.Threading.ThreadStart(Clicking));
            thread_click.Start();
        }

        public void HookStart()
        {
            IntPtr hInstance = LoadLibrary("User32");

            if (hKeyboardHook == 0)
            {
                // 创建HookProc实例
                KeyboardHookProcedure = new HookProc(KeyboardHookProc);

                // 设置钩子
                hKeyboardHook = SetWindowsHookEx(13, KeyboardHookProcedure, Marshal.GetHINSTANCE(
                    Assembly.GetExecutingAssembly().GetModules()[0]),
                    0);
                Console.WriteLine("Complete!");
                // 如果设置钩子失败
                if (hKeyboardHook == 0)
                {
                    HookStop();
                    throw new Exception("SetWindowsHookEx failed.");
                }
            }
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        public void HookStop()
        {
            bool retKeyboard = true;

            if (hKeyboardHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
            }


            if (!retKeyboard) throw new Exception("UnhookWindowsHookEx failed.");
        }

        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if(vkCode==32)
                {
                    HookStop();
                    thread_click.Abort();
                    thread_stop_attitude = true;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    SetThreadExecutionState(ExecutionFlag.Continus);
                    label1.Text = "退出任务！";
                }
            }
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }


        private void Start_Click()
        {
            SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Display | ExecutionFlag.Continus);
            HookStart();
        }

        private bool is_setpos = false;
        private int posint = 0;
        public Point[] points = new Point[4];
        Point mp = new Point();
        private void button2_Click(object sender, EventArgs e)
        {
            if(!is_setpos)
            {
                label1.Text = "按下空格确定坐标,不要点击鼠标，顺序为\n录音按键、回听按键、回放按键、继续/暂停按键。";
                is_setpos = true;
                posint = 0;
                button1.Enabled = false;
            }
            else
            {
                GetCursorPos(out mp);
                points[posint] = mp;
                label1.Text = points[posint].ToString();
                posint++;
                if (posint == 4)
                {
                    is_setpos = false;
                    label1.Text = "设置完成！";
                    button1.Enabled = true;
                }
            }


        }

        public void Clicking()
        {
            while(true)
            {
                if(thread_stop_attitude)
                {
                    return;
                }
                SetCursorPos(points[0].X, points[0].Y);
                Thread.Sleep(2000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(65000);

                SetCursorPos(points[1].X, points[1].Y);
                Thread.Sleep(2000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(65000);

                SetCursorPos(points[2].X, points[2].Y);
                Thread.Sleep(5000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(5000);

                SetCursorPos(points[0].X, points[0].Y);
                Thread.Sleep(2000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(65000);

                SetCursorPos(points[1].X, points[1].Y);
                Thread.Sleep(2000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(65000);

                SetCursorPos(points[2].X, points[2].Y);
                Thread.Sleep(5000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(5000);

                SetCursorPos(points[3].X, points[3].Y);
                Thread.Sleep(5000);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(5000);
            }
        }
        
    }
}
