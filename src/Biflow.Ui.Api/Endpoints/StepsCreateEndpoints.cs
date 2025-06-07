namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class StepsCreateEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("/{jobId:guid}/steps/agentjob", async (Guid jobId, AgentJobStepDto stepDto,
                LinkGenerator linker, HttpContext ctx,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateAgentJobStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<AgentJobStep>(StatusCodes.Status201Created)
            .WithSummary("Create agent job step")
            .WithDescription("Create a new SQL Server Agent job step")
            .WithName("CreateAgentJobStep");
        
        group.MapPost("/{jobId:guid}/steps/databricks", async (Guid jobId, DatabricksStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateDatabricksStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DatabricksStep>(StatusCodes.Status201Created)
            .WithSummary("Create Databricks step")
            .WithDescription("Create a new Databricks step")
            .WithName("CreateDatabricksStep");
        
        group.MapPost("/{jobId:guid}/steps/dataflow", async (Guid jobId, DataflowStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateDataflowStepCommand
                {
                    JobId = jobId,
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
                    AzureCredentialId = stepDto.AzureCredentialId,
                    WorkspaceId = stepDto.WorkspaceId,
                    WorkspaceName = stepDto.WorkspaceName,
                    DataflowId = stepDto.DataflowId,
                    DataflowName = stepDto.DataflowName,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DataflowStep>(StatusCodes.Status201Created)
            .WithSummary("Create Dataflow step")
            .WithDescription("Create a new Power BI/Fabric Dataflow (Gen 1 or Gen 2) step")
            .WithName("CreateDataflowStep");
        
        group.MapPost("/{jobId:guid}/steps/dataset", async (Guid jobId, DatasetStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateDatasetStepCommand
                {
                    JobId = jobId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    AzureCredentialId = stepDto.AzureCredentialId,
                    WorkspaceId = stepDto.WorkspaceId,
                    WorkspaceName = stepDto.WorkspaceName,
                    DatasetId = stepDto.DatasetId,
                    DatasetName = stepDto.DatasetName,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DatasetStep>(StatusCodes.Status201Created)
            .WithSummary("Create Dataset step")
            .WithDescription("Create a new Power BI semantic model refresh step")
            .WithName("CreateDatasetStep");
        
        group.MapPost("/{jobId:guid}/steps/dbt", async (Guid jobId, DbtStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateDbtStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DbtStep>(StatusCodes.Status201Created)
            .WithSummary("Create dbt step")
            .WithDescription("Create a new dbt Cloud job step")
            .WithName("CreateDbtStep");
        
        group.MapPost("/{jobId:guid}/steps/email", async (Guid jobId, EmailStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateEmailStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<EmailStep>(StatusCodes.Status201Created)
            .WithSummary("Create email step")
            .WithDescription("Create a new email step")
            .WithName("CreateEmailStep");
        
        group.MapPost("/{jobId:guid}/steps/exe", async (Guid jobId, ExeStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateExeStepCommand
                {
                    JobId = jobId,
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
                    ProxyId = stepDto.ProxyId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<ExeStep>(StatusCodes.Status201Created)
            .WithSummary("Create exe step")
            .WithDescription("Create a new executable file step")
            .WithName("CreateExeStep");
        
        group.MapPost("/{jobId:guid}/steps/fabric", async (Guid jobId, FabricStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateFabricStepCommand
                {
                    JobId = jobId,
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
                    WorkspaceName = stepDto.WorkspaceName,
                    ItemType = stepDto.ItemType,
                    ItemId = stepDto.ItemId,
                    ItemName = stepDto.ItemName,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<FabricStep>(StatusCodes.Status201Created)
            .WithSummary("Create Fabric step")
            .WithDescription("Create a new Fabric item step")
            .WithName("CreateFabricStep");
        
        group.MapPost("/{jobId:guid}/steps/function", async (Guid jobId, FunctionStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateFunctionStepCommand
                {
                    JobId = jobId,
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
                    FunctionInputFormat = stepDto.FunctionInputFormat,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<FunctionStep>(StatusCodes.Status201Created)
            .WithSummary("Create function step")
            .WithDescription("Create a new Azure Functions step")
            .WithName("CreateFunctionStep");
        
        group.MapPost("/{jobId:guid}/steps/job", async (Guid jobId, JobStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateJobStepParameter(
                        p.AssignToJobParameterId,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateJobStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<JobStep>(StatusCodes.Status201Created)
            .WithSummary("Create job step")
            .WithDescription("Create a new job step")
            .WithName("CreateJobStep");
        
        group.MapPost("/{jobId:guid}/steps/package", async (Guid jobId, PackageStepDto stepDto,
                LinkGenerator linker, HttpContext ctx,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreatePackageStepParameter(
                        p.ParameterName,
                        p.ParameterLevel,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreatePackageStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<PackageStep>(StatusCodes.Status201Created)
            .WithSummary("Create package step")
            .WithDescription("Create a new SSIS package step")
            .WithName("CreatePackageStep");
        
        group.MapPost("/{jobId:guid}/steps/pipeline", async (Guid jobId, PipelineStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreatePipelineStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<PipelineStep>(StatusCodes.Status201Created)
            .WithSummary("Create pipeline step")
            .WithDescription("Create a new pipeline step")
            .WithName("CreatePipelineStep");
        
        group.MapPost("/{jobId:guid}/steps/qlik", async (Guid jobId, QlikStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateQlikStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<QlikStep>(StatusCodes.Status201Created)
            .WithSummary("Create Qlik step")
            .WithDescription("Create a new Qlik Cloud app reload/automation run step")
            .WithName("CreateQlikStep");
        
        group.MapPost("/{jobId:guid}/steps/scd", async (Guid jobId, ScdStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateScdStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<ScdStep>(StatusCodes.Status201Created)
            .WithSummary("Create SCD table step")
            .WithDescription("Create a new SCD table load step")
            .WithName("CreateScdStep");
        
        group.MapPost("/{jobId:guid}/steps/sql", async (Guid jobId, SqlStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateSqlStepCommand
                {
                    JobId = jobId,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<SqlStep>(StatusCodes.Status201Created)
            .WithSummary("Create SQL step")
            .WithDescription("Create a new SQL step")
            .WithName("CreateSqlStep");
        
        group.MapPost("/{jobId:guid}/steps/tabular", async (Guid jobId, TabularStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var command = new CreateTabularStepCommand
                {
                    JobId = jobId,
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
                    ModelName = stepDto.ModelName,
                    TableName = stepDto.TableName,
                    PartitionName = stepDto.PartitionName,
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
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<TabularStep>(StatusCodes.Status201Created)
            .WithSummary("Create tabular step")
            .WithDescription("Create a new SQL Server Analysis Services tabular processing step")
            .WithName("CreateTabularStep");
    }
}