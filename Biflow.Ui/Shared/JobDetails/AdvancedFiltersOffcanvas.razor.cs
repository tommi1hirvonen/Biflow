using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;

namespace Biflow.Ui.Shared.JobDetails;

public partial class AdvancedFiltersOffcanvas : ComponentBase
{
    [Parameter] public EventCallback OnFiltersChanged { get; set; }

    [Parameter] public IEnumerable<FunctionApp> FunctionApps { get; set; } = Enumerable.Empty<FunctionApp>();

    [Parameter] public IEnumerable<PipelineClient> PipelineClients { get; set; } = Enumerable.Empty<PipelineClient>();

    [Parameter] public IEnumerable<ConnectionInfoBase> Connections { get; set; } = Enumerable.Empty<ConnectionInfoBase>();

    private HxOffcanvas? Offcanvas { get; set; }

    private HashSet<ConnectionInfoBase> ConnectionsFilter { get; set; } = new();

    private string PackageFolder { get; set; } = "";
    private string PackageProject { get; set; } = "";
    private string PackageName { get; set; } = "";

    private string PipelineName { get; set; } = "";
    private HashSet<PipelineClient> PipelineClientsFilter { get; set; } = new();

    private string FunctionUrl { get; set; } = "";
    private string FunctionInput { get; set; } = "";
    private HashSet<FunctionApp> FunctionAppsFilter { get; set; } = new();

    private string ExeFilePath { get; set; } = "";
    private string ExeArguments { get; set; } = "";

    public async Task ClearAsync()
    {
        ConnectionsFilter.Clear();
        PackageFolder = "";
        PackageProject = "";
        PackageName = "";
        PipelineName = "";
        PipelineClientsFilter.Clear();
        FunctionUrl = "";
        FunctionInput = "";
        FunctionAppsFilter.Clear();
        ExeFilePath = "";
        ExeArguments = "";
        await OnFiltersChanged.InvokeAsync();
    }

    public bool EvaluatePredicates(Step step) =>
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

    private bool ConnectionsPredicate(Step step) =>
        !ConnectionsFilter.Any() || step is IHasConnection hc && ConnectionsFilter.Select(c => c.ConnectionId).Contains(hc.ConnectionId ?? Guid.Empty);

    private bool PackageFolderPredicate(Step step) =>
        !PackageFolder.Any() || step is PackageStep p && (p.PackageFolderName?.ContainsIgnoreCase(PackageFolder) ?? false);
    private bool PackageProjectPredicate(Step step) =>
        !PackageProject.Any() || step is PackageStep p && (p.PackageProjectName?.ContainsIgnoreCase(PackageProject) ?? false);
    private bool PackageNamePredicate(Step step) =>
        !PackageName.Any() || step is PackageStep p && (p.PackageName?.ContainsIgnoreCase(PackageName) ?? false);

    private bool PipelineNamePredicate(Step step) =>
        !PipelineName.Any() || step is PipelineStep p && (p.PipelineName?.ContainsIgnoreCase(PipelineName) ?? false);
    private bool PipelineClientsPredicate(Step step) =>
        !PipelineClientsFilter.Any() || step is PipelineStep p && PipelineClientsFilter.Select(c => c.PipelineClientId).Contains(p.PipelineClientId ?? Guid.Empty);

    private bool FunctionUrlPredicate(Step step) =>
        !FunctionUrl.Any() || step is FunctionStep f && (f.FunctionUrl?.ContainsIgnoreCase(FunctionUrl) ?? false);
    private bool FunctionInputPredicate(Step step) =>
        !FunctionInput.Any() || step is FunctionStep f && (f.FunctionInput?.ContainsIgnoreCase(FunctionInput) ?? false);
    private bool FunctionAppsPredicate(Step step) =>
        !FunctionAppsFilter.Any() || step is FunctionStep f && FunctionAppsFilter.Select(a => a.FunctionAppId).Contains(f.FunctionAppId ?? Guid.Empty);

    private bool ExeFilePathPredicate(Step step) =>
        !ExeFilePath.Any() || step is ExeStep e && (e.ExeFileName?.ContainsIgnoreCase(ExeFilePath) ?? false);
    private bool ExeArgumentsPredicate(Step step) =>
        !ExeArguments.Any() || step is ExeStep e && (e.ExeArguments?.ContainsIgnoreCase(ExeArguments) ?? false);

    public Task ShowAsync() => Offcanvas?.ShowAsync() ?? Task.CompletedTask;
}
