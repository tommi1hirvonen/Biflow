namespace Biflow.Ui.Shared;

public class UserState
{
    public Dictionary<Guid, ExpandStatus> JobCategoryExpandStatuses { get; } = new();

    public Dictionary<Guid, ExpandStatus> DataTableCategoryExpandStatuses { get; } = new();
}

public class ExpandStatus
{
    public bool IsExpanded { get; set; } = true;
}