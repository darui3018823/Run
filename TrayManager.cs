using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Run;

internal sealed class TrayManager : IDisposable
{
    private readonly NotifyIcon _notify;
    private ToolStripMenuItem? _startupItem;

    public TrayManager(Action onOpen, Action onToggleStartup, Action onExit)
    {
        _notify = new NotifyIcon
        {
            Text    = "Run",
            Visible = true,
            Icon    = ExtractTrayIcon(),
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => onOpen());
        menu.Items.Add(new ToolStripSeparator());

        _startupItem = new ToolStripMenuItem("Run at Startup")
        {
            Checked = StartupManager.IsEnabled()
        };
        _startupItem.Click += (_, _) => onToggleStartup();
        menu.Items.Add(_startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => onExit());

        _notify.ContextMenuStrip = menu;
        _notify.DoubleClick     += (_, _) => onOpen();
    }

    public void UpdateStartupCheck(bool enabled)
    {
        if (_startupItem is not null)
            _startupItem.Checked = enabled;
    }

    public void ReRegister()
    {
        _notify.Visible = false;
        _notify.Visible = true;
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }

    private static Icon ExtractTrayIcon()
    {
        try
        {
            var system   = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var imageres = Path.Combine(system, "imageres.dll");
            var shell32  = Path.Combine(system, "shell32.dll");

            var small = new IntPtr[1];
            uint count = NativeMethods.ExtractIconEx(imageres, 95, null, small, 1);
            if (count == 0 || small[0] == IntPtr.Zero)
                count = NativeMethods.ExtractIconEx(shell32, 1, null, small, 1);

            if (count > 0 && small[0] != IntPtr.Zero)
                return Icon.FromHandle(small[0]);
        }
        catch { }
        return SystemIcons.Application;
    }
}
