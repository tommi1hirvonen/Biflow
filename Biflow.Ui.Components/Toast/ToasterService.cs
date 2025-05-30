﻿namespace Biflow.Ui.Components;

public class ToasterService
{
    internal event Action<ToastMessage>? OnMessage;

    public event Action? OnClear;

    private const int DefaultAutoHideDelay = 4000;

    public void AddInformation(string? message, int? autoHideDelay = DefaultAutoHideDelay) => AddInformation(null, message, autoHideDelay);

    public void AddInformation(string? title, string? message, int? autoHideDelay = DefaultAutoHideDelay)
    {
        var toastMessage = new ToastMessage
        {
            Title = title,
            Text = message,
            AutohideDelay = autoHideDelay,
            Color = ComponentColor.Primary
        };
        AddMessage(toastMessage);
    }

    public void AddSuccess(string? message, int? autoHideDelay = DefaultAutoHideDelay) => AddSuccess(null, message, autoHideDelay);

    public void AddSuccess(string? title, string? message, int? autoHideDelay = DefaultAutoHideDelay)
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

    public void AddInformationAction(string? title, string? buttonText, Action buttonAction) =>
        AddInformationAction(title, null, buttonText, buttonAction);

    public void AddInformationAction(string? title, string? text, string? buttonText, Action buttonAction)
    {
        var message = new ToastActionMessage
        {
            Title = title,
            Text = text,
            Color = ComponentColor.Primary,
            ButtonText = buttonText,
            ButtonColor = ButtonColor.Link,
            ButtonAction = buttonAction
        };
        AddMessage(message);
    }

    public void AddSuccessAction(string? title, string? buttonText, Action buttonAction) =>
        AddSuccessAction(title, null, buttonText, buttonAction);

    public void AddSuccessAction(string? title, string? text, string? buttonText, Action buttonAction)
    {
        var message = new ToastActionMessage
        {
            Title = title,
            Text = text,
            Color = ComponentColor.Success,
            ButtonText = buttonText,
            ButtonColor = ButtonColor.Link,
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
