namespace Biflow.Ui.Components;

public class ToastActionMessage : ToastMessage
{
    public string? ButtonText { get; set; }

    public ButtonColor ButtonColor { get; set; }

    public Action ButtonAction { get; set; } = () => { };
}
