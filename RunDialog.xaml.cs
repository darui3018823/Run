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

    private void SetDialogIcon()
    {
        var shell32 = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");

        // Index 24 = monitor/PC icon in Windows 11 shell32.dll
        var large = new IntPtr[1];
        uint count = NativeMethods.ExtractIconEx(shell32, 24, large, null, 1);
        if (count == 0 || large[0] == IntPtr.Zero)
        {
            // Fallback: try index 3 (application icon)
            count = NativeMethods.ExtractIconEx(shell32, 3, large, null, 1);
        }

        if (count > 0 && large[0] != IntPtr.Zero)
        {
            try
            {
                // IconBitmapDecoder preserves full alpha transparency (unlike CreateBitmapSourceFromHIcon)
                using var icon = System.Drawing.Icon.FromHandle(large[0]);
                using var ms = new MemoryStream();
                icon.Save(ms);
                ms.Position = 0;
                var decoder = new IconBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                RunIcon.Source = decoder.Frames[0];
            }
            finally
            {
                NativeMethods.DestroyIcon(large[0]);
            }
        }
    }

    private TextBox? EditBox =>
        CommandBox.Template.FindName("PART_EditableTextBox", CommandBox) as TextBox;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position: bottom-left of work area, just above taskbar
        var work = SystemParameters.WorkArea;
        Left = work.Left + 40;
        Top  = work.Bottom - ActualHeight - 20;

        _historyIndex = -1;
        CommandBox.ItemsSource = _history.Items;
        CommandBox.Focus();
        Dispatcher.BeginInvoke(() => EditBox?.SelectAll());
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
            bool elevate = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift))
                        == (ModifierKeys.Control | ModifierKeys.Shift);
            Execute(CommandBox.Text, elevate);
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
