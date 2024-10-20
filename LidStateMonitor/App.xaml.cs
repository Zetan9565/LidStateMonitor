using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Application = System.Windows.Forms.Application;

namespace LidStateMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private ProcessIcon icon;

        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid, int flags);

        internal struct PowerbroadcastSetting
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        private Guid guidLidswitchStateChange = new Guid(0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79, 0xE6, 0xA0, 0xF3);
        private const int DeviceNotifyWindowHandle = 0x00000000;
        private const int WmPowerbroadcast = 0x0218;
        private const int PbtPowersettingchange = 0x8013;

        private bool? prevLidState;

        public static Settings settings = new Settings();
        public static string SettingsPath => Path.Combine(Path.GetFileNameWithoutExtension(Application.ExecutablePath) + "Settings");

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            new Mutex(true, Process.GetCurrentProcess().ProcessName, out bool ret);
            if (!ret)
            {
                MessageBox.Show("已经有一个实例在运行", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(0);
            }

            icon = new ProcessIcon();
            icon.Display();

            InitializePowerSettingNotification();

            try
            {
                var fs = File.Open(SettingsPath, FileMode.Open);
                if (fs.Length > 0)
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    settings = bf.Deserialize(fs) as Settings;
                    fs.Close();
                }
                else settings = new Settings();
            }
            catch { settings = new Settings(); }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            icon.Dispose();
        }

        private void InitializePowerSettingNotification()
        {
            var stubWindow = new Window { Visibility = Visibility.Hidden, Height = 0, Width = 0, WindowStyle = WindowStyle.None, WindowState = WindowState.Minimized };
            stubWindow.Show();
            var hwnd = new WindowInteropHelper(stubWindow).Handle;
            RegisterPowerSettingNotification(hwnd, ref guidLidswitchStateChange, DeviceNotifyWindowHandle);

            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
            stubWindow.Hide();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmPowerbroadcast) OnPowerBroadcast(wParam, lParam);
            return IntPtr.Zero;
        }

        private void OnPowerBroadcast(IntPtr wParam, IntPtr lParam)
        {
            if ((int)wParam != PbtPowersettingchange) return;

            var ps = (PowerbroadcastSetting)Marshal.PtrToStructure(lParam, typeof(PowerbroadcastSetting));
            if (ps.PowerSetting != guidLidswitchStateChange) return;

            var isLidOpen = ps.Data != 0;

            if (!isLidOpen == prevLidState) OnLidStateChanged(isLidOpen);

            prevLidState = isLidOpen;
        }

        public static void OnLidStateChanged(bool isLidOpen)
        {
            if (isLidOpen) Execute(settings.OpenPath, settings.OpenArgs);
            else Execute(settings.ClosePath, settings.CloseArgs);
        }

        public static void Execute(string path, string args)
        {
            if (string.IsNullOrEmpty(path)) return;
            var process = new Process();
            try
            {
                if (path.IndexOf("\\") == 0) path = Application.StartupPath + path;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
                process.StartInfo.Arguments = args;
                process.StartInfo.FileName = path;
                if (settings.AsAdmin) process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = settings.NoWindow;
                process.Start();
                process.WaitForExit();
                process.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show($"LidStateMonitor: {e.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                process.Close();
            }
        }
    }

    [Serializable]
    public class Settings
    {
        public string OpenPath = string.Empty;
        public string OpenArgs = string.Empty;
        public string ClosePath = string.Empty;
        public string CloseArgs = string.Empty;
        public bool AsAdmin;
        public bool NoWindow;
    }
}
