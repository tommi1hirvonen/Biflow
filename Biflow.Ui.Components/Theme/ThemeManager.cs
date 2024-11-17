using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Biflow.Ui.Components;

public class ThemeManager(IJSRuntime js, ThemeService themeService) : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? jsObject;

    private DotNetObjectReference<ThemeManager>? dotNetObject;

    public async Task ToggleThemeAsync(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(jsObject);
        var themeName = theme switch
        {
            Theme.Light => "light",
            Theme.Dark => "dark",
            _ => throw new ArgumentException("Unrecognized theme value", nameof(theme))
        };
        _ = await jsObject.InvokeAsync<string>("setTheme", themeName);
        themeService.SetTheme(theme, false);
    }

    public async Task ToggleThemeAutoAsync()
    {
        ArgumentNullException.ThrowIfNull(jsObject);
        var effectiveTheme = await jsObject.InvokeAsync<string>("setTheme", "auto");
        var theme = effectiveTheme switch
        {
            "light" => Theme.Light,
            "dark" => Theme.Dark,
            _ => throw new ArgumentException($"Unrecognized theme value {effectiveTheme}", nameof(effectiveTheme))
        };
        themeService.SetTheme(theme, true);
    }

    protected override void OnInitialized()
    {
        dotNetObject = DotNetObjectReference.Create(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ArgumentNullException.ThrowIfNull(dotNetObject);
            jsObject = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Biflow.Ui.Components/ThemeManager.js");
            await jsObject.InvokeVoidAsync("setPreferredThemeChangedListener", dotNetObject);
            var preferredTheme = await jsObject.InvokeAsync<string>("getPreferredTheme");
            var effectiveTheme = await jsObject.InvokeAsync<string>("setTheme", preferredTheme);
            var theme = effectiveTheme switch
            {
                "light" => Theme.Light,
                "dark" => Theme.Dark,
                _ => throw new InvalidDataException($"Unrecognized theme value {effectiveTheme}")
            };
            themeService.SetTheme(theme, preferredTheme == "auto");
        }
    }

    [JSInvokable]
    public void UpdateTheme(string themeValue)
    {
        var theme = themeValue switch
        {
            "light" => Theme.Light,
            "dark" => Theme.Dark,
            _ => throw new ArgumentException($"Unrecognized theme value {themeValue}", nameof(themeValue))
        };
        themeService.SetTheme(theme, true);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (jsObject is not null)
                await jsObject.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        dotNetObject?.Dispose();
    }
}
