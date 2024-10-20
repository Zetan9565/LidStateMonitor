using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using Application = System.Windows.Forms.Application;

namespace LidStateMonitor
{
    public partial class SettingsBox
    {
        private const string shortcutPath = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\盖子状态监测";
        private readonly bool pause;

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
            var fi = new FileInfo(shortcutPath);
            AutoRun.IsChecked = fi.Exists && fi.Attributes.HasFlag(FileAttributes.ReparsePoint);
            pause = false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (OpenPath.Text != App.settings.OpenPath || OpenArgs.Text != App.settings.OpenArgs || ClosePath.Text != App.settings.ClosePath || CloseArgs.Text != App.settings.CloseArgs || !AsAdmin.IsChecked.Equals(App.settings.AsAdmin) || !NoWindow.IsChecked.Equals(App.settings.NoWindow))
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
            if (!fi.Exists || !fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
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
            var fi = new FileInfo(shortcutPath);
            if (fi.Exists && fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
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
