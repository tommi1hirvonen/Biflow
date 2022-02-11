using Microsoft.JSInterop;

namespace Biflow.Ui;

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
