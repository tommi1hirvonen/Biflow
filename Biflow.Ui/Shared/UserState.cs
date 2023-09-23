using Biflow.DataAccess.Models;

namespace Biflow.Ui.Shared;

public class UserState
{
    public Dictionary<Guid, ExpandStatus> DataTableCategoryExpandStatuses { get; } = new();
}

public class ExpandStatus
{
    public bool IsExpanded { get; set; } = true;
}