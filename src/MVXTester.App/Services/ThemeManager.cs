using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MVXTester.App.Services;

public static class ThemeManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MVXTester", "theme.txt");

    public static bool IsDarkTheme { get; private set; } = true;

    public static event Action? ThemeChanged;

    public static void Initialize()
    {
        var isDark = LoadSetting();
        ApplyTheme(isDark);
    }

    public static void ToggleTheme()
    {
        ApplyTheme(!IsDarkTheme);
    }

    public static void ApplyTheme(bool isDark)
    {
        IsDarkTheme = isDark;
        var themePath = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

        var app = Application.Current;
        if (app == null) return;

        var newTheme = new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        };

        var merged = app.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(newTheme);

        SaveSetting(isDark);

        ThemeChanged?.Invoke();
    }

    private static bool LoadSetting()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return File.ReadAllText(SettingsPath).Trim() == "dark";
        }
        catch { }
        return true; // default dark
    }

    private static void SaveSetting(bool isDark)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, isDark ? "dark" : "light");
        }
        catch { }
    }
}
