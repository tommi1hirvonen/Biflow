using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Biflow.Ui.Components;

public class ContextMenuService
{
    internal event ContextMenuShowHandler? OnShowContextMenu;

    internal delegate Task ContextMenuShowHandler(MouseEventArgs e, RenderFragment? fragment);

    internal async Task ShowContextMenuAsync(MouseEventArgs e, RenderFragment? menu)
    {
        if (OnShowContextMenu is not null)
        {
            await OnShowContextMenu(e, menu);
        }        
    }
}
