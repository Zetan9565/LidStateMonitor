using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using Application = System.Windows.Forms.Application;

namespace LidStateMonitor
{
    public partial class SettingsBox
    {
        private const string shortcutPath = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\盖子状态监测";
        private readonly bool pause;

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private const uint FILE_READ_EA = 0x0008;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string filename,
                [MarshalAs(UnmanagedType.U4)] uint access,
                [MarshalAs(UnmanagedType.U4)] FileShare share,
                IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                IntPtr templateFile);

        private static string GetFinalPathName(string path)
        {
            var h = CreateFile(path,
                FILE_READ_EA,
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);
            if (h == INVALID_HANDLE_VALUE)
                throw new Win32Exception();

            try
            {
                var sb = new StringBuilder(1024);
                var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                if (res == 0)
                    throw new Win32Exception();
                sb.Replace("\\\\?\\", string.Empty);
                return sb.ToString();
            }
            finally
            {
                CloseHandle(h);
            }
        }

        public SettingsBox()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.NoResize;
            OpenPath.Text = App.settings.OpenPath;
            OpenArgs.Text = App.settings.OpenArgs;
            ClosePath.Text = App.settings.ClosePath;
            CloseArgs.Text = App.settings.CloseArgs;
            AsAdmin.IsChecked = App.settings.AsAdmin;
            NoWindow.IsChecked = App.settings.NoWindow;
            pause = true;
            AutoRun.IsChecked = CheckAutoRun();
            pause = false;
        }

        private static bool CheckAutoRun()
        {
            return new FileInfo(shortcutPath).Exists && GetFinalPathName(shortcutPath) == Application.ExecutablePath;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (OpenPath.Text != App.settings.OpenPath || OpenArgs.Text != App.settings.OpenArgs || ClosePath.Text != App.settings.ClosePath || CloseArgs.Text != App.settings.CloseArgs || AsAdmin.IsChecked != App.settings.AsAdmin || NoWindow.IsChecked != App.settings.NoWindow)
                if (MessageBox.Show("修改尚未保存，确定退出吗？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.Cancel) e.Cancel = true;
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectFile(path =>
            {
                if (path.IndexOf(Application.StartupPath) == 0) OpenPath.Text = path.Remove(0, Application.StartupPath.Length);
                else OpenPath.Text = path;
            });
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectFile(path =>
            {
                if (path.IndexOf(Application.StartupPath) == 0) ClosePath.Text = path.Remove(0, Application.StartupPath.Length);
                else ClosePath.Text = path;
            });
        }

        private void SelectFile(Action<string> callback)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Title = "请选择可执行文件";
            ofd.Filter = "可执行文件(*.exe,*.bat,*.cmd)|*.exe;*.bat;*.cmd";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                callback.Invoke(ofd.FileName);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (OpenPath.Text != App.settings.OpenPath || OpenArgs.Text != App.settings.OpenArgs || ClosePath.Text != App.settings.ClosePath || CloseArgs.Text != App.settings.CloseArgs || !AsAdmin.IsChecked.Equals(App.settings.AsAdmin) || !NoWindow.IsChecked.Equals(App.settings.NoWindow))
                {
                    App.settings.OpenPath = OpenPath.Text;
                    App.settings.OpenArgs = OpenArgs.Text;
                    App.settings.ClosePath = ClosePath.Text;
                    App.settings.CloseArgs = CloseArgs.Text;
                    App.settings.AsAdmin = AsAdmin.IsChecked ?? false;
                    App.settings.NoWindow = NoWindow.IsChecked ?? false;
                    using var fs = File.Open(App.SettingsPath, FileMode.Create);
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, App.settings);
                    MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else MessageBox.Show("无修改", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("保存失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void OpenTestBtn_Click(object sender, RoutedEventArgs e)
        {
            Test(OpenPath.Text);
        }

        private void CloseTestBtn_Click(object sender, RoutedEventArgs e)
        {
            Test(ClosePath.Text);
        }

        private void Test(string path)
        {
            if (path.IndexOf("\\") == 0) path = Application.StartupPath + path;
            if (File.Exists(path)) App.Execute(path, OpenArgs.Text);
            else MessageBox.Show("可执行文件不存在，请检查文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void AutoRun_Checked(object sender, RoutedEventArgs e)
        {
            if (pause) return;
            var fi = new FileInfo(shortcutPath);
            if (!fi.Exists || !CheckAutoRun())
            {
                var process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c mklink \"{shortcutPath}\" \"{Application.ExecutablePath}\"";
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                process.Close();
                MessageBox.Show("已设置开机自启动", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else MessageBox.Show("已检测到开机启动项，无需重复设置", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void AutoRun_Unchecked(object sender, RoutedEventArgs e)
        {
            if (pause) return;
            if (CheckAutoRun())
            {
                var process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c del /f \"{shortcutPath}\"";
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                process.Close();
                MessageBox.Show("已取消开机自启动", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else MessageBox.Show("未检测到开机启动项，无需取消", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
