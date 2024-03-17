namespace Biflow.Ui.Shared;

public class UserState
{
    public Dictionary<Guid, ExpandStatus> DataTableCategoryExpandStatuses { get; } = [];
}

public class ExpandStatus
{
    public bool IsExpanded { get; set; } = true;
}