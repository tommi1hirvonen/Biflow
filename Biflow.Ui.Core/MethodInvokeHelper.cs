using Microsoft.JSInterop;

namespace Biflow.Ui.Core;

/// <summary>
/// Can be instantiated inside Blazor components to help with JS interop and calling component instance methods from JS.
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-dotnet-from-javascript?view=aspnetcore-7.0#component-instance-net-method-helper-class
/// </summary>
public class MethodInvokeHelper
{
    private readonly Action<string> action;

    public MethodInvokeHelper(Action<string> action)
    {
        this.action = action;
    }

    [JSInvokable]
    public void HelperInvokeCaller(string text)
    {
        action.Invoke(text);
    }
}
