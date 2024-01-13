namespace Biflow.Ui.Shared.JobDetails;

public partial class AdvancedFiltersOffcanvas : ComponentBase
{
    [Parameter] public EventCallback OnFiltersChanged { get; set; }

    [Parameter] public IEnumerable<FunctionApp> FunctionApps { get; set; } = Enumerable.Empty<FunctionApp>();

    [Parameter] public IEnumerable<PipelineClient> PipelineClients { get; set; } = Enumerable.Empty<PipelineClient>();

    [Parameter] public IEnumerable<ConnectionInfoBase> Connections { get; set; } = Enumerable.Empty<ConnectionInfoBase>();

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

    private readonly HashSet<ConnectionInfoBase> connectionsFilter = [];
    private readonly HashSet<FunctionApp> functionAppsFilter = [];
    private readonly HashSet<PipelineClient> pipelineClientsFilter = [];
    private readonly Dictionary<StepType, bool> expandedSections = [];

    private HxOffcanvas? offcanvas;

    public async Task ClearAsync()
    {
        connectionsFilter.Clear();
        Description = "";
        SqlStatement = "";
        PackageFolder = "";
        PackageProject = "";
        PackageName = "";
        PipelineName = "";
        pipelineClientsFilter.Clear();
        FunctionUrl = "";
        FunctionInput = "";
        functionAppsFilter.Clear();
        ExeFilePath = "";
        ExeArguments = "";
        await OnFiltersChanged.InvokeAsync();
    }

    public bool EvaluatePredicates(Step step) =>
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

    private bool DescriptionPredicate(Step step) =>
        string.IsNullOrEmpty(Description) || (step.StepDescription?.ContainsIgnoreCase(Description) ?? false);

    private bool SqlStatementPredicate(Step step) =>
        string.IsNullOrEmpty(SqlStatement) || step is SqlStep sql && sql.SqlStatement.ContainsIgnoreCase(SqlStatement);

    private bool ConnectionsPredicate(Step step) =>
        connectionsFilter.Count == 0 || step is IHasConnection hc && connectionsFilter.Select(c => c.ConnectionId).Contains(hc.ConnectionId);

    private bool PackageFolderPredicate(Step step) =>
        PackageFolder.Length == 0 || step is PackageStep p && (p.PackageFolderName?.ContainsIgnoreCase(PackageFolder) ?? false);
    private bool PackageProjectPredicate(Step step) =>
        PackageProject.Length == 0 || step is PackageStep p && (p.PackageProjectName?.ContainsIgnoreCase(PackageProject) ?? false);
    private bool PackageNamePredicate(Step step) =>
        PackageName.Length == 0 || step is PackageStep p && (p.PackageName?.ContainsIgnoreCase(PackageName) ?? false);

    private bool PipelineNamePredicate(Step step) =>
        PipelineName.Length == 0 || step is PipelineStep p && (p.PipelineName?.ContainsIgnoreCase(PipelineName) ?? false);
    private bool PipelineClientsPredicate(Step step) =>
        pipelineClientsFilter.Count == 0 || step is PipelineStep p && pipelineClientsFilter.Select(c => c.PipelineClientId).Contains(p.PipelineClientId);

    private bool FunctionUrlPredicate(Step step) =>
        FunctionUrl.Length == 0 || step is FunctionStep f && f.FunctionUrl.ContainsIgnoreCase(FunctionUrl);
    private bool FunctionInputPredicate(Step step) =>
        FunctionInput.Length == 0 || step is FunctionStep f && (f.FunctionInput?.ContainsIgnoreCase(FunctionInput) ?? false);
    private bool FunctionAppsPredicate(Step step) =>
        functionAppsFilter.Count == 0 || step is FunctionStep f && functionAppsFilter.Select(a => a.FunctionAppId).Contains(f.FunctionAppId);

    private bool ExeFilePathPredicate(Step step) =>
        ExeFilePath.Length == 0 || step is ExeStep e && (e.ExeFileName?.ContainsIgnoreCase(ExeFilePath) ?? false);
    private bool ExeArgumentsPredicate(Step step) =>
        ExeArguments.Length == 0 || step is ExeStep e && (e.ExeArguments?.ContainsIgnoreCase(ExeArguments) ?? false);

    public Task ShowAsync() => offcanvas?.ShowAsync() ?? Task.CompletedTask;
}
