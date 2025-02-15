using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="Snapshot">The snapshot object to which the environment should be reverted</param>
/// <param name="RetainIntegrationProperties">Whether to retain previous integration properties.
/// This should normally be set to true if transferring snapshots between environments (e.g. from test to prod)
/// where integration property values for the same entity may be different (e.g. connection strings or resource names).
/// </param>
public record RevertVersionCommand(EnvironmentSnapshot Snapshot, bool RetainIntegrationProperties)
    : IRequest<RevertVersionResponse>;

/// <summary>
/// 
/// </summary>
/// <param name="VersionId">The version id of the snapshot to which the environment should be reverted</param>
/// <param name="RetainIntegrationProperties">Whether to retain previous integration properties.
/// This should normally be set to true if transferring snapshots between environments (e.g. from test to prod)
/// where integration property values for the same entity may be different (e.g. connection strings or resource names).
/// </param>
public record RevertVersionByIdCommand(int VersionId, bool RetainIntegrationProperties)
    : IRequest<RevertVersionResponse>;

/// <summary>
/// 
/// </summary>
/// <param name="NewIntegrations">Integrations that were added as part of the revert process.
/// The property values of these integration entities should be checked after the revert
/// as they may need to be filled in.</param>
public record RevertVersionResponse(IReadOnlyList<(Type Type, string Name)> NewIntegrations);

[UsedImplicitly]
internal class RevertVersionByIdCommandHandler(IDbContextFactory<RevertDbContext> dbContextFactory, IMediator mediator)
    : IRequestHandler<RevertVersionByIdCommand, RevertVersionResponse>
{
    private readonly IRequestHandler<RevertVersionCommand, RevertVersionResponse> _handler =
        mediator.GetRequestHandler<RevertVersionCommand, RevertVersionResponse>();
    
    public async Task<RevertVersionResponse> Handle(RevertVersionByIdCommand request,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var snapshotJson = await context.EnvironmentVersions
            .AsNoTracking()
            .Where(v => v.VersionId == request.VersionId)
            .Select(v => v.SnapshotWithReferencesPreserved)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException<EnvironmentSnapshot>(request.VersionId);
        var snapshot = EnvironmentSnapshot.FromJson(snapshotJson, referencesPreserved: true);
        ArgumentNullException.ThrowIfNull(snapshot);
        var command = new RevertVersionCommand(snapshot, request.RetainIntegrationProperties);
        var response = await _handler.Handle(command, cancellationToken);
        return response;
    }
}

[UsedImplicitly]
internal class RevertVersionCommandHandler(
    IDbContextFactory<RevertDbContext> dbContextFactory,
    ISchedulerService schedulerService,
    ILogger<RevertVersionCommandHandler> logger)
    : IRequestHandler<RevertVersionCommand, RevertVersionResponse>
{
    public async Task<RevertVersionResponse> Handle(RevertVersionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = request.Snapshot;

            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Manually controlling transactions is allowed since
            // RevertDbContext does not use retry-on-failure execution strategy.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);


            // Capture subscriptions, job and data table authorizations.
            // These get automatically deleted when jobs, steps, tags and data tables are deleted.

            var capturedSubscriptions = await context.Subscriptions
                .AsNoTracking()
                .ToArrayAsync(cancellationToken);

            var capturedUsers = await context.Users
                .Include(u => u.Jobs)
                .Include(u => u.DataTables)
                .ToArrayAsync(cancellationToken);

            // Handle integration entities.
            // If RetainIntegrationProperties == true, use previous connection values for all integrations.
            // Otherwise, capture some sensitive properties and fill missing data for entities coming from the snapshot.
            // The snapshot does not contain data for properties marked as sensitive.

            // Collect integrations that may need to be manually updated with sensitive properties.
            var newIntegrations = new List<(Type Type, string Name)>();
            
            var capturedAsConnections = await context.AnalysisServicesConnections
                .AsNoTracking()
                .Select(c => new { c.ConnectionId, c.ConnectionString })
                .ToArrayAsync(cancellationToken);
            
            var capturedCredentials = await context.Credentials
                .AsNoTracking()
                .Select(c => new { c.CredentialId, c.Domain, c.Username, c.Password })
                .ToArrayAsync(cancellationToken);
            
            var capturedDatabricksWorkspaces = await context.DatabricksWorkspaces
                .AsNoTracking()
                .Select(w => new { w.WorkspaceId, w.WorkspaceUrl, w.ApiToken })
                .ToArrayAsync(cancellationToken);
            
            var capturedDbtAccounts = await context.DbtAccounts
                .AsNoTracking()
                .Select(a => new { a.DbtAccountId, a.AccountId, a.ApiBaseUrl, a.ApiToken })
                .ToArrayAsync(cancellationToken);
            
            var capturedFunctionApps = await context.FunctionApps
                .AsNoTracking()
                .Select(f => new
                {
                    f.FunctionAppId, f.SubscriptionId, f.ResourceGroupName, f.ResourceName, f.FunctionAppKey
                })
                .ToArrayAsync(cancellationToken);

            var capturedMsSqlConnections = await context.MsSqlConnections
                .AsNoTracking()
                .Select(c => new { c.ConnectionId, c.ConnectionString, c.CredentialId, c.ExecutePackagesAsLogin })
                .ToArrayAsync(cancellationToken);
            
            var capturedSnowflakeConnections = await context.SnowflakeConnections
                .AsNoTracking()
                .Select(c => new { c.ConnectionId, c.ConnectionString })
                .ToArrayAsync(cancellationToken);

            var capturedServicePrincipals = await context.ServicePrincipalCredentials
                .AsNoTracking()
                .Select(a => new { a.AzureCredentialId, a.TenantId, a.ClientId, a.ClientSecret })
                .ToArrayAsync(cancellationToken);

            var capturedOrganizationalAccounts = await context.OrganizationalAccountCredentials
                .AsNoTracking()
                .Select(a => new { a.AzureCredentialId, a.TenantId, a.ClientId, a.Username, a.Password })
                .ToArrayAsync(cancellationToken);

            var capturedManagedIdentities = await context.ManagedIdentityCredentials
                .AsNoTracking()
                .Select(c => new { c.AzureCredentialId, c.AzureCredentialName })
                .ToArrayAsync(cancellationToken);

            var capturedDataFactories = await context.DataFactories
                .AsNoTracking()
                .Select(x => new
                {
                    x.PipelineClientId, x.SubscriptionId, x.ResourceGroupName, x.ResourceName
                })
                .ToArrayAsync(cancellationToken);

            var capturedSynapseWorkspaces = await context.SynapseWorkspaces
                .AsNoTracking()
                .Select(x => new { x.PipelineClientId, x.SynapseWorkspaceUrl })
                .ToArrayAsync(cancellationToken);
            
            var capturedQlikEnvs = await context.QlikCloudEnvironments
                .AsNoTracking()
                .Select(q => new { q.QlikCloudEnvironmentId, q.EnvironmentUrl, q.ApiToken })
                .ToArrayAsync(cancellationToken);
            
            var capturedBlobStorages = await context.BlobStorageClients
                .AsNoTracking()
                .Select(b => new
                {
                    b.BlobStorageClientId,
                    b.ConnectionMethod,
                    b.StorageAccountUrl,
                    b.ConnectionString,
                    b.AzureCredentialId
                })
                .ToArrayAsync(cancellationToken);

            foreach (var connection in snapshot.AnalysisServicesConnections)
            {
                var match = capturedAsConnections.FirstOrDefault(c => c.ConnectionId == connection.ConnectionId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the connection string is reset.
                        connection.ConnectionString = "";
                    }
                    newIntegrations.Add((connection.GetType(), connection.ConnectionName));
                    continue;
                }
                if (request.RetainIntegrationProperties || string.IsNullOrEmpty(connection.ConnectionString))
                {
                    connection.ConnectionString = match.ConnectionString;
                }
            }

            foreach (var credential in snapshot.Credentials)
            {
                var match = capturedCredentials.FirstOrDefault(c => c.CredentialId == credential.CredentialId);
                if (match is null)
                {
                    newIntegrations.Add((credential.GetType(), credential.DisplayName));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    credential.Domain = match.Domain;
                    credential.Username = match.Username;
                    credential.Password = match.Password;
                }
                else if (string.IsNullOrEmpty(credential.Password))
                {
                    credential.Password = match.Password;
                }
            }
            
            foreach (var connection in snapshot.SqlConnections.OfType<MsSqlConnection>())
            {
                var match = capturedMsSqlConnections.FirstOrDefault(c => c.ConnectionId == connection.ConnectionId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the connection string is reset.
                        connection.ConnectionString = "";
                    }
                    newIntegrations.Add((connection.GetType(), connection.ConnectionName));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    connection.ConnectionString = match.ConnectionString;
                    connection.CredentialId =
                        match.CredentialId is { } id && snapshot.Credentials.Any(x => x.CredentialId == id) 
                            ? match.CredentialId 
                            : null;
                    connection.ExecutePackagesAsLogin = match.ExecutePackagesAsLogin;
                }
                else if (string.IsNullOrEmpty(connection.ConnectionString))
                {
                    connection.ConnectionString = match.ConnectionString;
                }
            }
            
            foreach (var connection in snapshot.SqlConnections.OfType<SnowflakeConnection>())
            {
                var match = capturedSnowflakeConnections.FirstOrDefault(c => c.ConnectionId == connection.ConnectionId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the connection string is reset.
                        connection.ConnectionString = "";
                    }
                    newIntegrations.Add((connection.GetType(), connection.ConnectionName));
                    continue;
                }
                if (request.RetainIntegrationProperties || string.IsNullOrEmpty(connection.ConnectionString))
                {
                    connection.ConnectionString = match.ConnectionString;
                }
            }

            foreach (var credential in snapshot.AzureCredentials.OfType<ServicePrincipalAzureCredential>())
            {
                var match = capturedServicePrincipals
                    .FirstOrDefault(a => a.AzureCredentialId == credential.AzureCredentialId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the service principal is reset.
                        credential.TenantId = "";
                        credential.ClientId = "";
                        credential.ClientSecret = "";
                    }
                    newIntegrations.Add((credential.GetType(), credential.AzureCredentialName ?? ""));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    credential.TenantId = match.TenantId;
                    credential.ClientId = match.ClientId;
                    credential.ClientSecret = match.ClientSecret;
                }
                else if (string.IsNullOrEmpty(credential.ClientSecret))
                {
                    credential.ClientSecret = match.ClientSecret;
                }
            }

            foreach (var credential in snapshot.AzureCredentials.OfType<OrganizationalAccountAzureCredential>())
            {
                var match = capturedOrganizationalAccounts
                    .FirstOrDefault(a => a.AzureCredentialId == credential.AzureCredentialId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the account is reset.
                        credential.TenantId = "";
                        credential.ClientId = "";
                        credential.Username = "";
                        credential.Password = "";
                    }
                    newIntegrations.Add((credential.GetType(), credential.AzureCredentialName ?? ""));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    credential.TenantId = match.TenantId;
                    credential.ClientId = match.ClientId;
                    credential.Username = match.Username;
                    credential.Password = match.Password;
                }
                else if (string.IsNullOrEmpty(credential.Password))
                {
                    credential.Password = match.Password;
                }
            }

            foreach (var credential in snapshot.AzureCredentials.OfType<ManagedIdentityAzureCredential>())
            {
                var match = capturedManagedIdentities.FirstOrDefault(c =>
                    c.AzureCredentialId == credential.AzureCredentialId);
                if (match is not null) continue;
                if (request.RetainIntegrationProperties)
                {
                    // Potential environment transfer without match => make sure the managed identity client id.
                    credential.ClientId = null;
                }
                newIntegrations.Add((credential.GetType(), credential.AzureCredentialName ?? ""));
            }
            
            foreach (var workspace in snapshot.DatabricksWorkspaces)
            {
                var match = capturedDatabricksWorkspaces.FirstOrDefault(w => w.WorkspaceId == workspace.WorkspaceId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the workspace is reset.
                        workspace.WorkspaceUrl = "";
                        workspace.ApiToken = "";
                    }
                    newIntegrations.Add((workspace.GetType(), workspace.WorkspaceName));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    workspace.WorkspaceUrl = match.WorkspaceUrl;
                    workspace.ApiToken = match.ApiToken;    
                }
                else if (string.IsNullOrEmpty(workspace.ApiToken))
                {
                    workspace.ApiToken = match.ApiToken;
                }
            }
            
            foreach (var account in snapshot.DbtAccounts)
            {
                var match = capturedDbtAccounts.FirstOrDefault(a => a.DbtAccountId == account.DbtAccountId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the account is reset.
                        account.AccountId = "";
                        account.ApiBaseUrl = "";
                        account.ApiToken = "";
                    }
                    newIntegrations.Add((account.GetType(), account.DbtAccountName));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    account.AccountId = match.AccountId;
                    account.ApiBaseUrl = match.ApiBaseUrl;
                    account.ApiToken = match.ApiToken;    
                }
                else if (string.IsNullOrEmpty(account.ApiToken))
                {
                    account.ApiToken = match.ApiToken;
                }
            }
            
            foreach (var func in snapshot.FunctionApps)
            {
                var match = capturedFunctionApps.FirstOrDefault(f => f.FunctionAppId == func.FunctionAppId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the function app is reset.
                        func.SubscriptionId = "";
                        func.ResourceGroupName = "";
                        func.ResourceName = "";
                        func.FunctionAppKey = "";
                    }
                    newIntegrations.Add((func.GetType(), func.FunctionAppName));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    func.SubscriptionId = match.SubscriptionId;
                    func.ResourceGroupName = match.ResourceGroupName;
                    func.ResourceName = match.ResourceName;
                    func.FunctionAppKey = match.FunctionAppKey;
                }
                else if (string.IsNullOrEmpty(func.FunctionAppKey))
                {
                    func.FunctionAppKey = match.FunctionAppKey;
                }
            }

            foreach (var df in snapshot.PipelineClients.OfType<DataFactory>())
            {
                var match = capturedDataFactories.FirstOrDefault(x => x.PipelineClientId == df.PipelineClientId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the Data Factory is reset.
                        df.SubscriptionId = "";
                        df.ResourceGroupName = "";
                        df.ResourceName = "";
                    }
                    newIntegrations.Add((df.GetType(), df.PipelineClientName));
                    continue;
                }
                if (!request.RetainIntegrationProperties)
                {
                    continue;
                }
                df.SubscriptionId = match.SubscriptionId;
                df.ResourceGroupName = match.ResourceGroupName;
                df.ResourceName = match.ResourceName;
            }

            foreach (var synapse in snapshot.PipelineClients.OfType<SynapseWorkspace>())
            {
                var match = capturedSynapseWorkspaces
                    .FirstOrDefault(x => x.PipelineClientId == synapse.PipelineClientId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the Synapse workspace is reset.
                        synapse.SynapseWorkspaceUrl = "";
                    }
                    newIntegrations.Add((synapse.GetType(), synapse.PipelineClientName));
                    continue;
                }
                if (!request.RetainIntegrationProperties)
                {
                    continue;
                }
                synapse.SynapseWorkspaceUrl = match.SynapseWorkspaceUrl;
            }
            
            foreach (var qlik in snapshot.QlikCloudEnvironments)
            {
                var match = capturedQlikEnvs
                    .FirstOrDefault(q => q.QlikCloudEnvironmentId == qlik.QlikCloudEnvironmentId);
                if (match is null)
                {
                    if (request.RetainIntegrationProperties)
                    {
                        // Potential environment transfer without match => make sure the Qlik env is reset.
                        qlik.EnvironmentUrl = "";
                        qlik.ApiToken = "";
                    }
                    newIntegrations.Add((qlik.GetType(), qlik.QlikCloudEnvironmentName));
                    continue;
                }
                if (request.RetainIntegrationProperties)
                {
                    qlik.EnvironmentUrl = match.EnvironmentUrl;
                    qlik.ApiToken = match.ApiToken;
                }
                else if (string.IsNullOrEmpty(qlik.ApiToken))
                {
                    qlik.ApiToken = match.ApiToken;
                }
            }
            
            foreach (var blob in snapshot.BlobStorageClients)
            {
                var match = capturedBlobStorages
                    .FirstOrDefault(b => b.BlobStorageClientId == blob.BlobStorageClientId
                                         && b.ConnectionMethod == blob.ConnectionMethod);
                if (match is null)
                {
                    newIntegrations.Add((blob.GetType(), blob.BlobStorageClientName));
                    continue;
                }
                switch (blob.ConnectionMethod)
                {
                    case BlobStorageConnectionMethod.AppRegistration:
                        if (request.RetainIntegrationProperties || string.IsNullOrEmpty(blob.StorageAccountUrl))
                        {
                            if (match.AzureCredentialId is { } id1
                                && snapshot.AzureCredentials.Any(c => c.AzureCredentialId == id1))
                            {
                                blob.UseCredential(id1, match.StorageAccountUrl ?? "");
                            }
                            else if (blob.AzureCredentialId is { } id2)
                            {
                                blob.UseCredential(id2, match.StorageAccountUrl ?? "");
                            }
                        }
                        break;
                    case BlobStorageConnectionMethod.ConnectionString:
                        if (request.RetainIntegrationProperties || string.IsNullOrEmpty(blob.ConnectionString))
                        {
                            blob.UseConnectionString(match.ConnectionString ?? "");
                        }
                        break;
                    case BlobStorageConnectionMethod.Url:
                        if (request.RetainIntegrationProperties || string.IsNullOrEmpty(blob.StorageAccountUrl))
                        {
                            blob.UseUrl(match.StorageAccountUrl ?? "");
                        }
                        break;
                }
            }

            var capturedFunctionStepKeys = await context.FunctionSteps
                .AsNoTracking()
                .Where(s => !string.IsNullOrEmpty(s.FunctionKey))
                .Select(s => new { s.StepId, s.FunctionKey })
                .ToArrayAsync(cancellationToken);
            
            var functionSteps = snapshot.Jobs.SelectMany(j =>
                j.Steps.OfType<FunctionStep>().Where(s => string.IsNullOrEmpty(s.FunctionKey)));
            foreach (var step in functionSteps)
            {
                step.FunctionKey = capturedFunctionStepKeys
                    .FirstOrDefault(s => s.StepId == step.StepId)
                    ?.FunctionKey;
            }


            // Delete all records for entities that are part of the revert process.
            // This guarantees that there are no clashes with unique db constraints, for example.

            // Clear the change tracker so that the previously captured entities do not change.
            context.ChangeTracker.Clear();

            var jobsToDelete = await context.Jobs
                .Include(j => j.JobParameters)
                .ThenInclude(j => j.AssigningStepParameters)
                .ThenInclude(p => p.Step)
                .Include(j => j.Steps)
                .ThenInclude(s => s.Dependencies)
                .Include(j => j.Steps)
                .ThenInclude(s => s.Depending)
                .Include($"{nameof(Job.Steps)}.{nameof(IHasStepParameters.StepParameters)}")
                .Include(j => j.JobSteps)
                .ToArrayAsync(cancellationToken);

            context.Jobs.RemoveRange(jobsToDelete);

            await context.SaveChangesAsync(cancellationToken);

            await context.DataObjects.ExecuteDeleteAsync(cancellationToken);

            await context.Tags.ExecuteDeleteAsync(cancellationToken);

            var dataTablesToDelete = await context.MasterDataTables
                .Include(t => t.Lookups)
                .Include(t => t.DependentLookups)
                .ToArrayAsync(cancellationToken);

            context.MasterDataTables.RemoveRange(dataTablesToDelete);

            await context.SaveChangesAsync(cancellationToken);

            await context.MasterDataTableCategories.ExecuteDeleteAsync(cancellationToken);
            
            await context.ScdTables.ExecuteDeleteAsync(cancellationToken);

            await context.SqlConnections.ExecuteDeleteAsync(cancellationToken);
            await context.AnalysisServicesConnections.ExecuteDeleteAsync(cancellationToken);
            await context.PipelineClients.ExecuteDeleteAsync(cancellationToken);
            await context.FunctionApps.ExecuteDeleteAsync(cancellationToken);
            await context.QlikCloudEnvironments.ExecuteDeleteAsync(cancellationToken);
            await context.DatabricksWorkspaces.ExecuteDeleteAsync(cancellationToken);
            await context.DbtAccounts.ExecuteDeleteAsync(cancellationToken);
            await context.BlobStorageClients.ExecuteDeleteAsync(cancellationToken);
            await context.Credentials.ExecuteDeleteAsync(cancellationToken);
            await context.AzureCredentials.ExecuteDeleteAsync(cancellationToken);

            
            // Add replacing entities from the snapshot.

            // Clear the change tracker so data from the db and snapshot do not interfere with each other.
            context.ChangeTracker.Clear();

            context.Credentials.AddRange(snapshot.Credentials);
            context.AzureCredentials.AddRange(snapshot.AzureCredentials);

            context.SqlConnections.AddRange(snapshot.SqlConnections);
            context.AnalysisServicesConnections.AddRange(snapshot.AnalysisServicesConnections);
            context.PipelineClients.AddRange(snapshot.PipelineClients);
            context.FunctionApps.AddRange(snapshot.FunctionApps);
            context.QlikCloudEnvironments.AddRange(snapshot.QlikCloudEnvironments);
            context.DatabricksWorkspaces.AddRange(snapshot.DatabricksWorkspaces);
            context.DbtAccounts.AddRange(snapshot.DbtAccounts);
            context.BlobStorageClients.AddRange(snapshot.BlobStorageClients);
            await context.SaveChangesAsync(cancellationToken);
            
            context.ScdTables.AddRange(snapshot.ScdTables);
            await context.SaveChangesAsync(cancellationToken);

            context.MasterDataTableCategories.AddRange(snapshot.DataTableCategories);
            await context.SaveChangesAsync(cancellationToken);
            context.MasterDataTables.AddRange(snapshot.DataTables);
            await context.SaveChangesAsync(cancellationToken);

            context.DataObjects.AddRange(snapshot.DataObjects);
            context.Tags.AddRange(snapshot.Tags);
            await context.SaveChangesAsync(cancellationToken);

            context.Jobs.AddRange(snapshot.Jobs);
            await context.SaveChangesAsync(cancellationToken);

            // Add subscriptions that were captured at the beginning and where jobs, steps and tags exist in the snapshot.

            context.ChangeTracker.Clear();

            var jobSubsToAdd = capturedSubscriptions
                .OfType<JobSubscription>()
                .Where(s => snapshot.Jobs.Any(j => j.JobId == s.JobId));

            var jobStepTagSubsToAdd = capturedSubscriptions
                .OfType<JobStepTagSubscription>()
                .Where(s => snapshot.Jobs.Any(j => j.JobId == s.JobId)
                            && snapshot.Tags.Any(t => t.TagId == s.TagId && t.TagType == TagType.Step));

            var stepSubsToAdd = capturedSubscriptions
                .OfType<StepSubscription>()
                .Where(sub => snapshot.Jobs.Any(job => job.Steps.Any(step => step.StepId == sub.StepId)));

            var stepTagSubsToAdd = capturedSubscriptions
                .OfType<StepTagSubscription>()
                .Where(s => snapshot.Tags.Any(t => t.TagId == s.TagId && t.TagType == TagType.Step));

            context.JobSubscriptions.AddRange(jobSubsToAdd);
            context.JobStepTagSubscriptions.AddRange(jobStepTagSubsToAdd);
            context.StepSubscriptions.AddRange(stepSubsToAdd);
            context.StepTagSubscriptions.AddRange(stepTagSubsToAdd);

            await context.SaveChangesAsync(cancellationToken);

            // Add user job and data table authorizations that were captured at the beginning
            // and where jobs and tables exist in the snapshot.

            context.ChangeTracker.Clear();

            var users = await context.Users
                .Include(u => u.Jobs)
                .Include(u => u.DataTables)
                .ToArrayAsync(cancellationToken);

            var jobs = await context.Jobs.ToArrayAsync(cancellationToken);
            var tables = await context.MasterDataTables.ToArrayAsync(cancellationToken);

            foreach (var user in users)
            {
                var capturedUser = capturedUsers.FirstOrDefault(u => u.UserId == user.UserId);
                if (capturedUser is null)
                {
                    continue;
                }
                foreach (var job in jobs.Where(j1 => capturedUser.Jobs.Any(j2 => j1.JobId == j2.JobId)))
                {
                    user.Jobs.Add(job);
                }
                foreach (var table in tables.Where(t1 => capturedUser.DataTables.Any(t2 => t1.DataTableId == t2.DataTableId)))
                {
                    user.DataTables.Add(table);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            // Commit if everything succeeded.
            // Transaction will auto-rollback when disposed if it hasn't been committed.
            await transaction.CommitAsync(cancellationToken);

            await schedulerService.SynchronizeAsync();

            return new RevertVersionResponse(newIntegrations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reverting version");
            throw;
        }
    }
}