using Microsoft.Win32;

namespace Run;

internal static class StartupManager
{
    private const string RegPath   = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "RunDialogReplacement";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath);
        return key?.GetValue(ValueName) is not null;
    }

    public static void Toggle()
    {
        if (IsEnabled()) Disable(); else Enable();
    }

    private static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true)!;
        var exePath = Environment.ProcessPath
                   ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
        key.SetValue(ValueName, exePath);
    }

    private static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true)!;
        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
