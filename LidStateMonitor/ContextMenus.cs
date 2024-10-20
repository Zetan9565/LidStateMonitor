using LidStateMonitor.Properties;
using System;
using System.Windows.Forms;

namespace LidStateMonitor
{
    public class ContextMenus
    {
        private bool isSettingsLoaded;

        public ContextMenuStrip Create()
        {
            var menu = new ContextMenuStrip();

            var item = new ToolStripMenuItem { Text = @"设置" };
            item.Click += Settings_Click;
            item.Image = Resources.Settings;
            menu.Items.Add(item);

            var sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            item = new ToolStripMenuItem { Text = @"退出" };
            item.Click += Exit_Click;
            item.Image = Resources.Exit;
            menu.Items.Add(item);

            return menu;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            if (isSettingsLoaded) return;

            isSettingsLoaded = true;
            new SettingsBox() { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen }.ShowDialog();
            isSettingsLoaded = false;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
