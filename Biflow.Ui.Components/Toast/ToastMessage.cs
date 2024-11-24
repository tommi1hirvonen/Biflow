namespace Biflow.Ui.Components;

public class ToastMessage
{
    public string Key { get; } = Guid.NewGuid().ToString();

    public int? AutohideDelay { get; init; }

    public string? Title { get; init; }

    public string? Text { get; init; }

    public ComponentColor Color { get; init; }

}