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



        //読み込み終了！下からメインの処理


        //選択中のプロセス
        public Process selectedProcess = null;


        public Form1()
        {
            InitializeComponent();

            //コンボボックス設定
            List<Process> comboBoxMyItems = new List<Process>();

            //ウィンドウのタイトルに「」を含むプロセスをすべて取得する
            Process[] ps = GetProcessesByWindow("", null);

            //windowの名前があるやつだけ入れる
            foreach (Process p in ps)
            {
                if (p.MainWindowTitle.Length > 1)
                {
                   
                    comboBoxMyItems.Add(p);
                }
            }

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

        private void button2_Click(object sender, EventArgs e)
        {
            SetForegroundWindow(selectedProcess.MainWindowHandle);
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;        //フォームの表示
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }

        private void contextMenuStrip1_Opening_1(object sender, CancelEventArgs e)
        {

        }
        private void 開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;        //フォームの表示
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }

        private void 閉じるToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;    //アイコンをトレイから取り除く
            //notifyIcon1.Dispose();
            Application.Exit();             //アプリケーションの終了
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("cant close");
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void 閉じるToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }
    }






    //辞書用のクラス
    class MyKeyindex
    {
        public int myKey_ { get; set; }
        public string myValue_ { get; set; }
        public MyKeyindex(int key, string value)
        {
            myKey_ = key;
            myValue_ = value;
        }
    }
}
