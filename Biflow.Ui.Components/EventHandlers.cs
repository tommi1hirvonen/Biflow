using Microsoft.AspNetCore.Components;

namespace Biflow.Ui.Components;

// Corresponding JS in wwwroot/Biflow.Ui.Components.lib.module.js
[EventHandler("onsearch", typeof(SearchEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}
