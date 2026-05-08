using Microsoft.Win32;
using System.Windows;
using Application = System.Windows.Application;

namespace Run;

public sealed class DarkModeWatcher
{
    private const string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public bool IsDark { get; private set; }

    public void Start()
    {
        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void Stop()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
            Application.Current.Dispatcher.Invoke(ApplyTheme);
    }

    public void ApplyTheme()
    {
        IsDark = ReadIsDark();
        var uri = IsDark
            ? new Uri("pack://application:,,,/Themes/Dark.xaml")
            : new Uri("pack://application:,,,/Themes/Light.xaml");

        var dict = new ResourceDictionary { Source = uri };
        var merged = Application.Current.Resources.MergedDictionaries;

        var existing = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("/Themes/") == true);
        if (existing != null)
            merged.Remove(existing);
        merged.Add(dict);
    }

    private static bool ReadIsDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath);
        var val = key?.GetValue("AppsUseLightTheme");
        return val is int i && i == 0;
    }
}
