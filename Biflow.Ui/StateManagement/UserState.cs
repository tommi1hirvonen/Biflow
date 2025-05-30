﻿namespace Biflow.Ui.StateManagement;

public class UserState
{
    public JobsPageState Jobs { get; } = new();

    public SchedulesPageState Schedules { get; } = new();

    public ExecutionsPageState Executions { get; } = new();

    public Dictionary<Guid, ExpandStatus> DataTableCategoryExpandStatuses { get; } = [];

    public VersionsPageState Versions { get; } = new();

    public StepEditState StepEdit { get; } = new();
}