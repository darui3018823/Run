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
            if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
            {
                var kbs = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                if (kbs.vkCode == NativeMethods.VK_R)
                {
                    bool winDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0
                                || (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;
                    if (winDown)
                    {
                        Application.Current.Dispatcher.BeginInvoke(_callback);
                        return new IntPtr(1);
                    }
                }
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
