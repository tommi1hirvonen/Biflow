namespace Biflow.Core.Constants;

public static class Scopes
{
    public const string JobsRead = "Jobs.Read";
    public const string JobsWrite = "Jobs.Write";
    
    public const string SchedulesRead = "Schedules.Read";
    public const string SchedulesWrite = "Schedules.Write";
    
    public const string ExecutionsRead = "Executions.Read";
    public const string ExecutionsWrite = "Executions.Write";
    
    public const string IntegrationsRead = "Integrations.Read";
    public const string IntegrationsWrite = "Integrations.Write";
    
    public const string DataTablesRead = "DataTables.Read";
    public const string DataTablesWrite = "DataTables.Write";
    
    public const string ScdTablesRead = "ScdTables.Read";
    public const string ScdTablesWrite = "ScdTables.Write";
    
    public const string EnvironmentVersionsRead = "EnvironmentVersions.Read";
    public const string EnvironmentVersionsWrite = "EnvironmentVersions.Write";
    
    public const string SubscriptionsRead = "Subscriptions.Read";
    public const string SubscriptionsWrite = "Subscriptions.Write";
    
    public const string UsersRead = "Users.Read";
    public const string UsersWrite = "Users.Write";

    public static IReadOnlyList<string> AsReadOnlyList() => AllScopes;

    private static readonly string[] AllScopes =
    [
        DataTablesRead,
        DataTablesWrite,
        EnvironmentVersionsRead,
        EnvironmentVersionsWrite,
        ExecutionsRead,
        ExecutionsWrite,
        IntegrationsRead,
        IntegrationsWrite,
        JobsRead,
        JobsWrite,
        ScdTablesRead,
        ScdTablesWrite,
        SchedulesRead,
        SchedulesWrite,
        SubscriptionsRead,
        SubscriptionsWrite,
        UsersRead,
        UsersWrite
    ];
}