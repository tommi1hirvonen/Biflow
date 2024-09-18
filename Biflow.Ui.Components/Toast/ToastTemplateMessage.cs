using Microsoft.AspNetCore.Components;

namespace Biflow.Ui.Components;

public class ToastTemplateMessage : ToastMessage
{
    public required RenderFragment RenderFragment { get; set; }
}