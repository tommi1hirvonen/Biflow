namespace Biflow.Ui.Components;

public class ToastMessage
{
    public string Key { get; } = Guid.NewGuid().ToString();

    public int? AutohideDelay { get; set; }

    public string? Title { get; set; }

    public string? Text { get; set; }

    public ComponentColor Color { get; set; }

}