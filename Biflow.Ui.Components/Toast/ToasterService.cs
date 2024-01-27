namespace Biflow.Ui.Components;

public class ToasterService
{
    internal event Action<ToastMessage>? OnMessage;

    public event Action? OnClear;

    public void AddInformation(string? message, int? autoHideDelay = 3000) => AddInformation(null, message, autoHideDelay);

    public void AddInformation(string? title, string? message, int? autoHideDelay = 3000)
    {
        var toastMessage = new ToastMessage
        {
            Title = title,
            Text = message,
            AutohideDelay = autoHideDelay
        };
        AddMessage(toastMessage);
    }

    public void AddSuccess(string? message, int? autoHideDelay = 3000) => AddSuccess(null, message, autoHideDelay);

    public void AddSuccess(string? title, string? message, int? autoHideDelay = 3000)
    {
        var toastMessage = new ToastMessage
        {
            Title = title,
            Text = message,
            AutohideDelay = autoHideDelay,
            Color = ComponentColor.Success
        };
        AddMessage(toastMessage);
    }

    public void AddWarning(string? message, int? autoHideDelay = null) => AddWarning(null, message, autoHideDelay);

    public void AddWarning(string? title, string? message, int? autoHideDelay = null)
    {
        var toastMessage = new ToastMessage
        {
            Title = title,
            Text = message,
            AutohideDelay = autoHideDelay,
            Color = ComponentColor.Warning
        };
        AddMessage(toastMessage);
    }

    public void AddError(string? message, int? autoHideDelay = null) => AddError(null, message, autoHideDelay);

    public void AddError(string? title, string? message, int? autoHideDelay = null)
    {
        var toastMessage = new ToastMessage
        {
            Title = title,
            Text = message,
            AutohideDelay = autoHideDelay,
            Color = ComponentColor.Danger
        };
        AddMessage(toastMessage);
    }

    public void AddAction(string? title, ButtonColor buttonColor, string? buttonText, Action buttonAction)
    {
        var message = new ToastActionMessage
        {
            Title = title,
            ButtonText = buttonText,
            ButtonColor = buttonColor,
            ButtonAction = buttonAction
        };
        AddMessage(message);
    }

    public void AddMessage(ToastMessage message)
    {
        OnMessage?.Invoke(message);
    }

    public void Clear()
    {
        OnClear?.Invoke();
    }
}
