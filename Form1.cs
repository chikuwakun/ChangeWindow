using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ChangeWindow.KeyboardHook;


namespace ChangeWindow
{
    public partial class Form1 : Form
    {
        // 諸々の読み込み
        [DllImport("USER32.DLL")]
        private static extern IntPtr GetForegroundWindow();


        [DllImport("USER32.DLL")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        private static string searchWindowText = null;
        private static string searchClassName = null;
        private static ArrayList foundProcessIds = null;
        private static ArrayList foundProcesses = null;

        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd,
            StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd,
            StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(
            IntPtr hWnd, out int lpdwProcessId);
        public static Process[] GetProcessesByWindow(
        string windowText, string className)
        {
            //検索の準備をする
            foundProcesses = new ArrayList();
            foundProcessIds = new ArrayList();
            searchWindowText = windowText;
            searchClassName = className;

            //ウィンドウを列挙して、対象のプロセスを探す
            EnumWindows(new EnumWindowsDelegate(EnumWindowCallBack), IntPtr.Zero);

            //結果を返す
            return (Process[])foundProcesses.ToArray(typeof(Process));
        }

        public static List<Process> GetProcessesOnlyWindow()
        {
          
            List<Process> foundProcesses = new List<Process>();

            //ウィンドウのタイトルに「」を含むプロセスをすべて取得する
            Process[] ps = GetProcessesByWindow("", null);

            //windowの名前があるプロセスだけをリストに入れる
            foreach (Process p in ps)
            {
                if (p.MainWindowTitle.Length > 1)
                {

                    foundProcesses.Add(p);
                }
            }

            return foundProcesses;

        }

        private static bool EnumWindowCallBack(IntPtr hWnd, IntPtr lparam)
        {
            if (searchWindowText != null)
            {
                //ウィンドウのタイトルの長さを取得する
                int textLen = GetWindowTextLength(hWnd);
                if (textLen == 0)
                {
                    //次のウィンドウを検索
                    return true;
                }
                //ウィンドウのタイトルを取得する
                StringBuilder tsb = new StringBuilder(textLen + 1);
                GetWindowText(hWnd, tsb, tsb.Capacity);
                //タイトルに指定された文字列を含むか
                if (tsb.ToString().IndexOf(searchWindowText) < 0)
                {
                    //含んでいない時は、次のウィンドウを検索
                    return true;
                }
            }

            if (searchClassName != null)
            {
                //ウィンドウのクラス名を取得する
                StringBuilder csb = new StringBuilder(256);
                GetClassName(hWnd, csb, csb.Capacity);
                //クラス名に指定された文字列を含むか
                if (csb.ToString().IndexOf(searchClassName) < 0)
                {
                    //含んでいない時は、次のウィンドウを検索
                    return true;
                }
            }

            //プロセスのIDを取得する
            int processId;
            GetWindowThreadProcessId(hWnd, out processId);
            //今まで見つかったプロセスでは無いことを確認する
            if (!foundProcessIds.Contains(processId))
            {
                foundProcessIds.Add(processId);
                //プロセスIDをからProcessオブジェクトを作成する
                foundProcesses.Add(Process.GetProcessById(processId));
            }

            //次のウィンドウを検索
            return true;
        }




        //keyboard Hook


        KeyboardHook keyboardHook = new KeyboardHook();

        private List<int> plessedKeys = new List<int>();

        protected override void OnLoad(EventArgs e)
        {
            keyboardHook.KeyDownEvent += KeyboardHook_KeyDownEvent;
            keyboardHook.KeyUpEvent += KeyboardHook_KeyUpEvent;
            keyboardHook.Hook();
        }


        public bool flagOn = false;
        public bool flagOff = false;
        IntPtr hWnd2;
        private void KeyboardHook_KeyDownEvent(object sender, KeyEventArg e)
        {
            plessedKeys.Add(e.KeyCode);

            if (plessedKeys.Contains(32) && plessedKeys.Contains(160))
            {
                //同時推ししたときにやりたい処理

                hWnd2 = GetForegroundWindow();
                int id;
                GetWindowThreadProcessId(hWnd2, out id);
                

                Console.WriteLine("hahaha");
                SetForegroundWindow(selectedProcess.MainWindowHandle);
                plessedKeys.Remove(32);
                plessedKeys.Remove(160);

                flagOn = true;
            }else if(flagOn == true)
            {
                flagOn = false;
                flagOff = true;
            }
        }

        private void KeyboardHook_KeyUpEvent(object sender, KeyEventArg e)
        {
            // キーが離されたときにやりたいこと
            if (plessedKeys.Contains(e.KeyCode))
            {
                plessedKeys.Remove(e.KeyCode);
            }

            if(flagOff== true)
            {
                flagOff = false;
                SetForegroundWindow(hWnd2);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            keyboardHook.UnHook();
        }





        //ここからメインの処理


        //選択中のプロセス
        public Process selectedProcess = null;


        public Form1()
        {
            InitializeComponent();

            //コンボボックス初期設定
            List<Process> comboBoxMyItems = GetProcessesOnlyWindow();

            //comboBoxの表示を指定
            comboBox1.DisplayMember = "MainWindowTitle";
            comboBox1.ValueMember = "Id";
            comboBox1.DataSource = comboBoxMyItems;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Process selected = (Process)comboBox1.SelectedItem;
            string selectedItem = selected.MainWindowTitle;
            label1.Text = selectedItem + "が選択されています。";
            selectedProcess = (Process)comboBox1.SelectedItem;
        }

        
        
        //タスクバーのアイコンダブルクリックでフォームが表示されるようにする
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;        //フォームの表示
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }


        //非表示ボタンを押すと非表示
        private void button3_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }
        
        //更新ボタンを押すとプロセスを再取得する
        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.DataSource = GetProcessesOnlyWindow();
        }
    }



    class KeyboardHook
    {
        protected const int WH_KEYBOARD_LL = 0x000D;
        protected const int WM_KEYDOWN = 0x0100;
        protected const int WM_KEYUP = 0x0101;
        protected const int WM_SYSKEYDOWN = 0x0104;
        protected const int WM_SYSKEYUP = 0x0105;

        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_SCANCODE = 0x0008,
            KEYEVENTF_UNICODE = 0x0004,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private KeyboardProc proc;
        private IntPtr hookId = IntPtr.Zero;

        public void Hook()
        {
            if (hookId == IntPtr.Zero)
            {
                proc = HookProcedure;
                using (var curProcess = Process.GetCurrentProcess())
                {
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
            }
        }

        public void UnHook()
        {
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }

        public IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                var vkCode = (int)kb.vkCode;
                OnKeyDownEvent(vkCode);
            }
            else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
            {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                var vkCode = (int)kb.vkCode;
                OnKeyUpEvent(vkCode);
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public delegate void KeyEventHandler(object sender, KeyEventArg e);
        public event KeyEventHandler KeyDownEvent;
        public event KeyEventHandler KeyUpEvent;

        protected void OnKeyDownEvent(int keyCode)
        {
            KeyDownEvent?.Invoke(this, new KeyEventArg(keyCode));
        }
        protected void OnKeyUpEvent(int keyCode)
        {
            KeyUpEvent?.Invoke(this, new KeyEventArg(keyCode));
        }

        public class KeyEventArg : EventArgs
        {
            public int KeyCode { get; }

            public KeyEventArg(int keyCode)
            {
                KeyCode = keyCode;
            }

        }
    }


}
