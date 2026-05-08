using System.Windows;
using System.Windows.Interop;

namespace Run;

internal sealed class MessageSinkWindow : Window
{
    private readonly TrayManager _tray;
    private HwndSource? _source;
    private readonly uint _wmTaskbarCreated;

    public MessageSinkWindow(TrayManager tray)
    {
        _tray = tray;
        _wmTaskbarCreated = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        Width = Height = 0;
        WindowStyle = WindowStyle.None;
        ShowInTaskbar = false;
        Visibility = Visibility.Hidden;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
    }

    protected override void OnClosed(EventArgs e)
    {
        _source?.RemoveHook(WndProc);
        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)msg == _wmTaskbarCreated)
            _tray.ReRegister();
        return IntPtr.Zero;
    }
}
