using System;
using System.Windows.Forms;
using LidStateMonitor.Properties;

namespace LidStateMonitor
{
    internal class ProcessIcon : IDisposable
    {
        private readonly NotifyIcon notifyIcon;

        public ProcessIcon()
        {
            notifyIcon = new NotifyIcon();
        }

        public void Display()
        {
            notifyIcon.MouseClick += OnIconClick;
            notifyIcon.Icon = Resources.LidCloseAndLock;
            notifyIcon.Text = @"笔记本盖子开合状态监测器";
            notifyIcon.Visible = true;

            notifyIcon.ContextMenuStrip = new ContextMenus().Create();
        }

        public void Dispose()
        {
            notifyIcon.Dispose();
        }

        private void OnIconClick(object sender, MouseEventArgs e)
        {
            //TODO 显示设置界面？
        }
    }
}
