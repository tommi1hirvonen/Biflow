namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class StepsUpdateEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPut("/steps/agentjob/{stepId:guid}", async (Guid stepId, AgentJobStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new UpdateAgentJobStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    ConnectionId = stepDto.ConnectionId,
                    AgentJobName = stepDto.AgentJobName,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<AgentJobStep>()
            .WithSummary("Update Agent job step")
            .WithDescription("Update an existing SQL Server Agent job step")
            .WithName("UpdateAgentJobStep");
        
        group.MapPut("/steps/databricks/{stepId:guid}", async (Guid stepId, DatabricksStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateDatabricksStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    DatabricksWorkspaceId = stepDto.DatabricksWorkspaceId,
                    DatabricksStepSettings = stepDto.Settings,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DatabricksStep>()
            .WithSummary("Update Databricks step")
            .WithDescription("Update an existing Databricks step")
            .WithName("UpdateDatabricksStep");
        
        group.MapPut("/steps/dataflow/{stepId:guid}", async (Guid stepId, DataflowStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new UpdateDataflowStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    WorkspaceId = stepDto.WorkspaceId,
                    DataflowId = stepDto.DataflowId,
                    AzureCredentialId = stepDto.AzureCredentialId,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DataflowStep>()
            .WithSummary("Update Dataflow step")
            .WithDescription("Update an existing Power BI/Fabric Dataflow step")
            .WithName("UpdateDataflowStep");
        
        group.MapPut("/steps/dataset/{stepId:guid}", async (Guid stepId, DatasetStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new UpdateDatasetStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    WorkspaceId = stepDto.WorkspaceId,
                    DatasetId = stepDto.DatasetId,
                    AzureCredentialId = stepDto.AzureCredentialId,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DatasetStep>()
            .WithSummary("Update Dataset step")
            .WithDescription("Update an existing Power BI semantic model refresh step")
            .WithName("UpdateDatasetStep");
        
        group.MapPut("/steps/dbt/{stepId:guid}", async (Guid stepId, DbtStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new UpdateDbtStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    DbtAccountId = stepDto.DbtAccountId,
                    DbtJob = stepDto.DbtJob,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DbtStep>()
            .WithSummary("Update dbt step")
            .WithDescription("Update an existing dbt Cloud job step")
            .WithName("UpdateDbtStep");
        
        group.MapPut("/steps/email/{stepId:guid}", async (Guid stepId, EmailStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateEmailStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    Recipients = stepDto.Recipients,
                    Subject = stepDto.Subject,
                    Body = stepDto.Body,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<EmailStep>()
            .WithSummary("Update email step")
            .WithDescription("Update an existing email step")
            .WithName("UpdateEmailStep");
        
        group.MapPut("/steps/exe/{stepId:guid}", async (Guid stepId, ExeStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateExeStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    FilePath = stepDto.FilePath,
                    Arguments = stepDto.Arguments,
                    WorkingDirectory = stepDto.WorkingDirectory,
                    SuccessExitCode = stepDto.SuccessExitCode,
                    RunAsCredentialId = stepDto.RunAsCredentialId,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<ExeStep>()
            .WithSummary("Update EXE step")
            .WithDescription("Update an existing executable file step")
            .WithName("UpdateExeStep");
        
        group.MapPut("/steps/fabric/{stepId:guid}", async (Guid stepId, FabricStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateFabricStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    WorkspaceId = stepDto.WorkspaceId,
                    ItemType = stepDto.ItemType,
                    ItemId = stepDto.ItemId,
                    AzureCredentialId = stepDto.AzureCredentialId,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<FabricStep>()
            .WithSummary("Update Fabric step")
            .WithDescription("Update an existing Fabric item run step")
            .WithName("UpdateFabricStep");
        
        group.MapPut("/steps/function/{stepId:guid}", async (Guid stepId, FunctionStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateFunctionStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    FunctionAppId = stepDto.FunctionAppId,
                    FunctionUrl = stepDto.FunctionUrl,
                    FunctionInput = stepDto.FunctionInput,
                    FunctionIsDurable = stepDto.FunctionIsDurable,
                    FunctionKey = stepDto.FunctionKey,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<FunctionStep>()
            .WithSummary("Update function step")
            .WithDescription("Update an existing Azure Functions step")
            .WithName("UpdateFunctionStep");
        
        group.MapPut("/steps/job/{stepId:guid}", async (Guid stepId, JobStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateJobStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    JobToExecuteId = stepDto.JobToExecuteId,
                    ExecuteSynchronized = stepDto.ExecuteSynchronized,
                    FilterStepTagIds = stepDto.FilterStepTagIds,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<JobStep>()
            .WithSummary("Update job step")
            .WithDescription("Update an existing job step")
            .WithName("UpdateJobStep");
        
        group.MapPut("/steps/package/{stepId:guid}", async (Guid stepId, PackageStepDto stepDto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdatePackageStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterLevel,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdatePackageStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    ConnectionId = stepDto.ConnectionId,
                    PackageFolderName = stepDto.PackageFolderName,
                    PackageProjectName = stepDto.PackageProjectName,
                    PackageName = stepDto.PackageName,
                    ExecuteIn32BitMode = stepDto.ExecuteIn32BitMode,
                    ExecuteAsLogin = stepDto.ExecuteAsLogin,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<PackageStep>()
            .WithSummary("Update package step")
            .WithDescription("Update an existing SSIS package step")
            .WithName("UpdatePackageStep");
        
        group.MapPut("/steps/pipeline/{stepId:guid}", async (Guid stepId, PipelineStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdatePipelineStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    PipelineClientId = stepDto.PipelineClientId,
                    PipelineName = stepDto.PipelineName,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<PipelineStep>()
            .WithSummary("Update pipeline step")
            .WithDescription("Update an existing pipeline step")
            .WithName("UpdatePipelineStep");
        
        group.MapPut("/steps/qlik/{stepId:guid}", async (Guid stepId, QlikStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new UpdateQlikStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    QlikCloudEnvironmentId = stepDto.QlikCloudEnvironmentId,
                    Settings = stepDto.Settings,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<QlikStep>()
            .WithSummary("Update Qlik step")
            .WithDescription("Update an existing Qlik Cloud step")
            .WithName("UpdateQlikStep");
        
        group.MapPut("/steps/scd/{stepId:guid}", async (Guid stepId, ScdStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new UpdateScdStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    ScdTableId = stepDto.ScdTableId,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<ScdStep>()
            .WithSummary("Update SCD step")
            .WithDescription("Update an existing SCD table load step")
            .WithName("UpdateScdStep");
        
        group.MapPut("/steps/sql/{stepId:guid}", async (Guid stepId, SqlStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new UpdateExecutionConditionParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateSqlStepCommand
                {
                    StepId = stepId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    SqlStatement = stepDto.SqlStatement,
                    ConnectionId = stepDto.ConnectionId,
                    ResultCaptureJobParameterId = stepDto.ResultCaptureJobParameterId,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters,
                    Sources = stepDto.Sources
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray(),
                    Targets = stepDto.Targets
                        .Select(x => new DataObjectRelation(x.DataObjectId, x.DataAttributes))
                        .ToArray()
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<SqlStep>()
            .WithSummary("Update SQL step")
            .WithDescription("Update an existing SQL step")
            .WithName("UpdateSqlStep");
    }
}