namespace Biflow.Ui.Components;

public class ToastActionMessage : ToastMessage
{
    public string? ButtonText { get; init; }

    public ButtonColor ButtonColor { get; init; }

    public Action ButtonAction { get; init; } = () => { };
}
