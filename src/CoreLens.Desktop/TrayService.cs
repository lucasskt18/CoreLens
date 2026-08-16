using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace CoreLens.Desktop;

internal sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;

    public TrayService(Action showDashboard, Action showMini, Action exit)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        _icon = new Forms.NotifyIcon
        {
            Text = "CoreLens",
            Visible = true,
            Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Dashboard", null, (_, _) => showDashboard());
        menu.Items.Add("Mini painel", null, (_, _) => showMini());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => exit());
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => showDashboard();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
