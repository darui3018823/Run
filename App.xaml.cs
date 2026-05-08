using System.Windows;
using Application = System.Windows.Application;

namespace Run;

public partial class App : Application
{
    private static System.Threading.Mutex? _mutex;
    private KeyboardHook? _hook;
    private TrayManager? _tray;
    private DarkModeWatcher? _darkMode;
    private MessageSinkWindow? _sink;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new System.Threading.Mutex(true, "RunDialogReplacement_{A3F1C2D4-8B7E-4F9A-B2C1-D5E6F7A8B9C0}", out bool isFirst);
        if (!isFirst)
        {
            _mutex.Dispose();
            Shutdown();
            return;
        }
        GC.KeepAlive(_mutex);

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _darkMode = new DarkModeWatcher();
        _darkMode.Start();

        _tray = new TrayManager(OnOpen, OnToggleStartup, OnExit);
        _sink = new MessageSinkWindow(_tray);
        _sink.Show();

        _hook = new KeyboardHook(OnWinR);
        _hook.Install();
    }

    private void OnWinR() => Dispatcher.BeginInvoke(ShowRunDialog);

    private void ShowRunDialog()
    {
        if (Windows.OfType<RunDialog>().FirstOrDefault() is { } existing)
        {
            existing.Activate();
            return;
        }
        var dlg = new RunDialog(_darkMode!, HistoryManager.Instance);
        dlg.Show();
    }

    private void OnOpen() => Dispatcher.Invoke(ShowRunDialog);

    private void OnToggleStartup()
    {
        StartupManager.Toggle();
        _tray?.UpdateStartupCheck(StartupManager.IsEnabled());
    }

    private void OnExit() => Dispatcher.Invoke(Shutdown);

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Uninstall();
        _tray?.Dispose();
        _darkMode?.Stop();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
