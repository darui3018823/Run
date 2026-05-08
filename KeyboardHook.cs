using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Application = System.Windows.Application;

namespace Run;

internal sealed class KeyboardHook : IDisposable
{
    private readonly Action _callback;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle;
    private bool _suppressWinKeyUp;

    public KeyboardHook(Action callback)
    {
        _callback = callback;
        _proc = HookCallback;
    }

    public void Install()
    {
        using var mod = Process.GetCurrentProcess().MainModule!;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeMethods.GetModuleHandle(mod.ModuleName),
            0);
    }

    public void Uninstall()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var kbs = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
            {
                if (kbs.vkCode == NativeMethods.VK_R)
                {
                    bool lwin = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0;
                    bool rwin = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;
                    if (lwin || rwin)
                    {
                        _suppressWinKeyUp = true;
                        byte winVk = lwin ? (byte)NativeMethods.VK_LWIN : (byte)NativeMethods.VK_RWIN;
                        // Inject an unassigned VK (0x88) between Win-down and Win-up.
                        // Shell opens Start Menu only when Win is pressed then released with no
                        // other key in between; the phantom key breaks that sequence.
                        NativeMethods.keybd_event(0x88, 0, 0, IntPtr.Zero);
                        NativeMethods.keybd_event(0x88, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
                        // Now inject Win KeyUp to clear the Win-held state (fixes Win+P etc.)
                        NativeMethods.keybd_event(winVk, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
                        Application.Current.Dispatcher.BeginInvoke(_callback);
                        return new IntPtr(1);
                    }
                }
            }
            else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
            {
                if (_suppressWinKeyUp &&
                    (kbs.vkCode == NativeMethods.VK_LWIN || kbs.vkCode == NativeMethods.VK_RWIN))
                {
                    bool injected = (kbs.flags & NativeMethods.LLKHF_INJECTED) != 0;
                    if (injected)
                        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                    _suppressWinKeyUp = false;
                    return new IntPtr(1);
                }
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
