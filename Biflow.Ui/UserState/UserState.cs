namespace Biflow.Ui.StateManagement;

public class UserState
{
    public JobsPageState Jobs { get; } = new();

    public SchedulesPageState Schedules { get; } = new();

    public Dictionary<Guid, ExpandStatus> DataTableCategoryExpandStatuses { get; } = [];
}