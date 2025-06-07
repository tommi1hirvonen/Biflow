namespace Biflow.Ui.Components;

public class ThemeService
{
    public event Action<Theme, bool>? OnThemeChanged;

    public Theme CurrentTheme { get; private set; }

    public bool IsAuto { get; private set; } = true;

    public void SetTheme(Theme theme, bool isAuto)
    {
        CurrentTheme = theme;
        IsAuto = isAuto;
        OnThemeChanged?.Invoke(CurrentTheme, isAuto);
    }
}
