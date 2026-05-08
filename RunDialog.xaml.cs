using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Run;

public partial class RunDialog : Window
{
    private readonly DarkModeWatcher _darkMode;
    private readonly HistoryManager _history;
    private int _historyIndex = -1;

    public RunDialog(DarkModeWatcher darkMode, HistoryManager history)
    {
        _darkMode = darkMode;
        _history = history;
        InitializeComponent();
        SetDialogIcon();
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        int dark = _darkMode.IsDark ? 1 : 0;
        NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
    }

    private void SetDialogIcon()
    {
        try
        {
            var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var imageres = Path.Combine(system, "imageres.dll");
            var shell32  = Path.Combine(system, "shell32.dll");

            var large = new IntPtr[1];
            uint count = NativeMethods.ExtractIconEx(imageres, 95, large, null, 1);
            if (count == 0 || large[0] == IntPtr.Zero)
                count = NativeMethods.ExtractIconEx(shell32, 3, large, null, 1);

            if (count > 0 && large[0] != IntPtr.Zero)
            {
                try
                {
                    using var icon = System.Drawing.Icon.FromHandle(large[0]);
                    using var bitmap = icon.ToBitmap();
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var frame = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    RunIcon.Source = frame;
                    Icon = frame;
                }
                finally
                {
                    NativeMethods.DestroyIcon(large[0]);
                }
            }
        }
        catch { }
    }

    private TextBox? EditBox =>
        CommandBox.Template.FindName("PART_EditableTextBox", CommandBox) as TextBox;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position: bottom-left of work area, just above taskbar
        var work = SystemParameters.WorkArea;
        Left = work.Left + 10;
        Top  = work.Bottom - ActualHeight - 8;

        _historyIndex = -1;
        CommandBox.ItemsSource = _history.Items;

        ForceForeground();
        CommandBox.Focus();

        Dispatcher.BeginInvoke(() =>
        {
            if (EditBox is { } tb)
            {
                tb.Background  = (System.Windows.Media.Brush)FindResource("InputBg");
                tb.Foreground  = System.Windows.Media.Brushes.Black;
                tb.CaretBrush  = System.Windows.Media.Brushes.Black;
                tb.SelectAll();
            }
        });
    }

    private void ForceForeground()
    {
        var hwnd   = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var fgHwnd = NativeMethods.GetForegroundWindow();
        var fgTid  = NativeMethods.GetWindowThreadProcessId(fgHwnd, IntPtr.Zero);
        var ourTid = NativeMethods.GetCurrentThreadId();

        if (fgTid != ourTid)
            NativeMethods.AttachThreadInput(fgTid, ourTid, true);

        NativeMethods.SetForegroundWindow(hwnd);

        if (fgTid != ourTid)
            NativeMethods.AttachThreadInput(fgTid, ourTid, false);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void CommandBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            var items = _history.Items;
            if (items.Count > 0)
            {
                _historyIndex = Math.Min(_historyIndex + 1, items.Count - 1);
                CommandBox.Text = items[_historyIndex];
                Dispatcher.BeginInvoke(() =>
                {
                    if (EditBox is { } tb) tb.CaretIndex = tb.Text.Length;
                });
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            _historyIndex = Math.Max(_historyIndex - 1, -1);
            CommandBox.Text = _historyIndex < 0 ? "" : _history.Items[_historyIndex];
            Dispatcher.BeginInvoke(() =>
            {
                if (EditBox is { } tb && tb.Text.Length > 0) tb.CaretIndex = tb.Text.Length;
            });
            e.Handled = true;
        }
        else if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt))
                    == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                ExecuteAsSystem(CommandBox.Text);
            }
            else
            {
                bool elevate = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift))
                            == (ModifierKeys.Control | ModifierKeys.Shift);
                Execute(CommandBox.Text, elevate);
            }
            e.Handled = true;
        }
    }

    private void CommandBox_KeyDown(object sender, KeyEventArgs e) { }

    private void OK_Click(object sender, RoutedEventArgs e)
        => Execute(CommandBox.Text, false);

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Programs (*.exe)|*.exe|All Files (*.*)|*.*",
            Title = "Browse"
        };
        if (dlg.ShowDialog() == true)
        {
            CommandBox.Text = dlg.FileName;
            Dispatcher.BeginInvoke(() =>
            {
                if (EditBox is { } tb) tb.CaretIndex = tb.Text.Length;
            });
        }
    }

    private void ExecuteAsSystem(string command)
    {
        command = command.Trim();
        if (string.IsNullOrEmpty(command)) return;

        var psexec = FindInPath("psexec.exe") ?? FindInPath("psexec");
        if (psexec == null)
        {
            MessageBox.Show("psexec.exe が見つかりません", "Run", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName        = psexec,
                Arguments       = $"-s -i {command}",
                UseShellExecute = true,
                Verb            = "runas"
            };
            System.Diagnostics.Process.Start(psi);
            _history.AddAndSave(command);
            Close();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) { }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Run", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? FindInPath(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                var full = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        return null;
    }

    private void Execute(string command, bool elevate)
    {
        command = command.Trim();
        if (string.IsNullOrEmpty(command)) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(command)
            {
                UseShellExecute = true
            };
            if (elevate) psi.Verb = "runas";
            System.Diagnostics.Process.Start(psi);
            _history.AddAndSave(command);
            Close();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // UAC cancelled by user
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Run", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
