namespace Biflow.Ui.Shared.JobDetails;

public partial class AdvancedFiltersOffcanvas : ComponentBase
{
    [Parameter] public EventCallback OnFiltersChanged { get; set; }

    [Parameter] public IEnumerable<FunctionApp> FunctionApps { get; set; } = [];

    [Parameter] public IEnumerable<PipelineClient> PipelineClients { get; set; } = [];

    [Parameter] public IEnumerable<SqlConnectionBase> Connections { get; set; } = [];

    [Parameter] public IEnumerable<int> ExecutionPhases { get; set; } = [];

    public string Description { get; private set; } = "";
    public string SqlStatement { get; private set; } = "";
    public string PackageFolder { get; private set; } = "";
    public string PackageProject { get; private set; } = "";
    public string PackageName { get; private set; } = "";
    public string PipelineName { get; private set; } = "";
    public string FunctionUrl { get; private set; } = "";
    private string FunctionInput { get; set; } = "";
    public string ExeFilePath { get; private set; } = "";
    public string ExeArguments { get; private set; } = "";

    private readonly HashSet<SqlConnectionBase> _connectionsFilter = [];
    private readonly HashSet<FunctionApp> _functionAppsFilter = [];
    private readonly HashSet<PipelineClient> _pipelineClientsFilter = [];
    private readonly HashSet<int> _executionPhasesFilter = [];
    private readonly Dictionary<StepType, bool> _expandedSections = [];

    private HxOffcanvas? _offcanvas;

    public async Task ClearAsync()
    {
        _executionPhasesFilter.Clear();
        _connectionsFilter.Clear();
        Description = "";
        SqlStatement = "";
        PackageFolder = "";
        PackageProject = "";
        PackageName = "";
        PipelineName = "";
        _pipelineClientsFilter.Clear();
        FunctionUrl = "";
        FunctionInput = "";
        _functionAppsFilter.Clear();
        ExeFilePath = "";
        ExeArguments = "";
        await OnFiltersChanged.InvokeAsync();
    }

    public bool EvaluatePredicates(Step step) =>
        ExecutionPhasePredicate(step) &&
        DescriptionPredicate(step) &&
        SqlStatementPredicate(step) &&
        ConnectionsPredicate(step) &&
        PackageFolderPredicate(step) &&
        PackageProjectPredicate(step) &&
        PackageNamePredicate(step) &&
        PipelineNamePredicate(step) &&
        PipelineClientsPredicate(step) &&
        FunctionUrlPredicate(step) &&
        FunctionInputPredicate(step) &&
        FunctionAppsPredicate(step) &&
        ExeFilePathPredicate(step) &&
        ExeArgumentsPredicate(step);

    private bool ExecutionPhasePredicate(Step step) =>
        _executionPhasesFilter.Count == 0 || _executionPhasesFilter.Contains(step.ExecutionPhase);

    private bool DescriptionPredicate(Step step) =>
        string.IsNullOrEmpty(Description) || (step.StepDescription?.ContainsIgnoreCase(Description) ?? false);

    private bool SqlStatementPredicate(Step step) =>
        string.IsNullOrEmpty(SqlStatement) || step is SqlStep sql && sql.SqlStatement.ContainsIgnoreCase(SqlStatement);

    private bool ConnectionsPredicate(Step step) =>
        _connectionsFilter.Count == 0 || step is IHasSqlConnection hc && _connectionsFilter.Select(c => c.ConnectionId).Contains(hc.ConnectionId);

    private bool PackageFolderPredicate(Step step) =>
        PackageFolder.Length == 0 || step is PackageStep p && (p.PackageFolderName?.ContainsIgnoreCase(PackageFolder) ?? false);
    private bool PackageProjectPredicate(Step step) =>
        PackageProject.Length == 0 || step is PackageStep p && (p.PackageProjectName?.ContainsIgnoreCase(PackageProject) ?? false);
    private bool PackageNamePredicate(Step step) =>
        PackageName.Length == 0 || step is PackageStep p && (p.PackageName?.ContainsIgnoreCase(PackageName) ?? false);

    private bool PipelineNamePredicate(Step step) =>
        PipelineName.Length == 0 || step is PipelineStep p && (p.PipelineName?.ContainsIgnoreCase(PipelineName) ?? false);
    private bool PipelineClientsPredicate(Step step) =>
        _pipelineClientsFilter.Count == 0 || step is PipelineStep p && _pipelineClientsFilter.Select(c => c.PipelineClientId).Contains(p.PipelineClientId);

    private bool FunctionUrlPredicate(Step step) =>
        FunctionUrl.Length == 0 || step is FunctionStep f && f.FunctionUrl.ContainsIgnoreCase(FunctionUrl);
    private bool FunctionInputPredicate(Step step) =>
        FunctionInput.Length == 0 || step is FunctionStep f && (f.FunctionInput?.ContainsIgnoreCase(FunctionInput) ?? false);
    private bool FunctionAppsPredicate(Step step) =>
        _functionAppsFilter.Count == 0 || step is FunctionStep f && _functionAppsFilter.Select(a => a.FunctionAppId).Contains(f.FunctionAppId);

    private bool ExeFilePathPredicate(Step step) =>
        ExeFilePath.Length == 0 || step is ExeStep e && (e.ExeFileName?.ContainsIgnoreCase(ExeFilePath) ?? false);
    private bool ExeArgumentsPredicate(Step step) =>
        ExeArguments.Length == 0 || step is ExeStep e && (e.ExeArguments?.ContainsIgnoreCase(ExeArguments) ?? false);

    public Task ShowAsync() => _offcanvas?.ShowAsync() ?? Task.CompletedTask;
}
