using System.Windows.Interop;

namespace Run;

internal sealed class KeyboardHook : IDisposable
{
    private readonly Action _callback;
    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _installed;

    private const int HotkeyId  = 1;
    private const int WM_HOTKEY = 0x0312;

    public KeyboardHook(Action callback)
    {
        _callback = callback;
    }

    public void Install(IntPtr hwnd)
    {
        _hwnd   = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);
        _installed = NativeMethods.RegisterHotKey(hwnd, HotkeyId, NativeMethods.MOD_WIN | NativeMethods.MOD_NOREPEAT, NativeMethods.VK_R);
    }

    public void Uninstall()
    {
        if (_installed)
        {
            NativeMethods.UnregisterHotKey(_hwnd, HotkeyId);
            _installed = false;
        }
        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _callback();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose() => Uninstall();
}
