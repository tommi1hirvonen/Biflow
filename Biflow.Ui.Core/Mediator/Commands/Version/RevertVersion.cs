using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

public record RevertVersionCommand(EnvironmentSnapshot Snapshot) : IRequest;

public record RevertVersionByIdCommand(int VersionId) : IRequest;

[UsedImplicitly]
internal class RevertVersionByIdCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory, IMediator mediator)
    : IRequestHandler<RevertVersionByIdCommand>
{
    public async Task Handle(RevertVersionByIdCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var snapshotJson = await context.EnvironmentVersions
            .AsNoTracking()
            .Where(v => v.VersionId == request.VersionId)
            .Select(v => v.SnapshotWithReferencesPreserved)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException<EnvironmentSnapshot>(request.VersionId);
        var snapshot = EnvironmentSnapshot.FromJson(snapshotJson, referencesPreserved: true);
        ArgumentNullException.ThrowIfNull(snapshot);
        var command = new RevertVersionCommand(snapshot);
        await mediator.SendAsync(command, cancellationToken);
    }
}

[UsedImplicitly]
internal class RevertVersionCommandHandler(
    IDbContextFactory<RevertDbContext> dbContextFactory,
    ISchedulerService schedulerService,
    ILogger<RevertVersionCommandHandler> logger)
    : IRequestHandler<RevertVersionCommand>
{
    public async Task Handle(RevertVersionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = request.Snapshot;

            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Manually controlling transactions is allowed since RevertDbContext does not use retry-on-failure execution strategy.
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


            // Capture some sensitive properties and fill missing data for entities coming from the snapshot.
            // The snapshot does not contain data for properties marked as sensitive.

            var capturedSqlConnectionStrings = await context.SqlConnections
                .AsNoTracking()
                .Select(c => new { c.ConnectionId, c.ConnectionString })
                .ToArrayAsync(cancellationToken);

            foreach (var connection in snapshot.SqlConnections.Where(c => string.IsNullOrEmpty(c.ConnectionString)))
            {
                connection.ConnectionString = capturedSqlConnectionStrings
                    .FirstOrDefault(c => c.ConnectionId == connection.ConnectionId)
                    ?.ConnectionString ?? "";
            }
            
            var capturedAsConnectionStrings = await context.AnalysisServicesConnections
                .AsNoTracking()
                .Select(c => new { c.ConnectionId, c.ConnectionString })
                .ToArrayAsync(cancellationToken);

            foreach (var connection in snapshot.AnalysisServicesConnections.Where(c => string.IsNullOrEmpty(c.ConnectionString)))
            {
                connection.ConnectionString = capturedAsConnectionStrings
                  .FirstOrDefault(c => c.ConnectionId == connection.ConnectionId)
                  ?.ConnectionString ?? "";
            }

            var capturedServicePrincipalSecrets = await context.ServicePrincipalCredentials
                .AsNoTracking()
                .Select(a => new { a.AzureCredentialId, a.ClientSecret })
                .ToArrayAsync(cancellationToken);

            foreach (var credential in snapshot.AzureCredentials
                         .OfType<ServicePrincipalAzureCredential>()
                         .Where(a => string.IsNullOrEmpty(a.ClientSecret)))
            {
                credential.ClientSecret = capturedServicePrincipalSecrets
                    .FirstOrDefault(a => a.AzureCredentialId == credential.AzureCredentialId)
                    ?.ClientSecret ?? "";
            }
            
            var capturedOrganizationalAccountPasswords = await context.OrganizationalAccountCredentials
                .AsNoTracking()
                .Select(a => new { a.AzureCredentialId, a.Password })
                .ToArrayAsync(cancellationToken);

            foreach (var credential in snapshot.AzureCredentials
                         .OfType<OrganizationalAccountAzureCredential>()
                         .Where(a => string.IsNullOrEmpty(a.Password)))
            {
                credential.Password = capturedOrganizationalAccountPasswords
                    .FirstOrDefault(a => a.AzureCredentialId == credential.AzureCredentialId)
                    ?.Password ?? "";
            }

            var capturedBlobStorages = await context.BlobStorageClients
                .AsNoTracking()
                .Select(b => new { b.BlobStorageClientId, b.ConnectionMethod, b.StorageAccountUrl, b.ConnectionString })
                .ToArrayAsync(cancellationToken);

            foreach (var blob in snapshot.BlobStorageClients
                .Where(b => b.ConnectionMethod == BlobStorageConnectionMethod.ConnectionString && string.IsNullOrEmpty(b.ConnectionString)))
            {
                var cs = capturedBlobStorages
                    .FirstOrDefault(b => b.BlobStorageClientId == blob.BlobStorageClientId && b.ConnectionMethod == BlobStorageConnectionMethod.ConnectionString)
                    ?.ConnectionString
                    ?? "";
                blob.UseConnectionString(cs);
            }

            foreach (var blob in snapshot.BlobStorageClients
                .Where(b => b.ConnectionMethod == BlobStorageConnectionMethod.Url && string.IsNullOrEmpty(b.StorageAccountUrl)))
            {
                var url = capturedBlobStorages
                    .FirstOrDefault(b => b.BlobStorageClientId == blob.BlobStorageClientId && b.ConnectionMethod == BlobStorageConnectionMethod.Url)
                    ?.StorageAccountUrl
                    ?? "";
                blob.UseUrl(url);
            }

            var capturedFunctionAppKeys = await context.FunctionApps
                .AsNoTracking()
                .Select(f => new { f.FunctionAppId, f.FunctionAppKey })
                .ToArrayAsync(cancellationToken);

            foreach (var func in snapshot.FunctionApps.Where(f => string.IsNullOrEmpty(f.FunctionAppKey)))
            {
                func.FunctionAppKey = capturedFunctionAppKeys
                    .FirstOrDefault(f => f.FunctionAppId == func.FunctionAppId)
                    ?.FunctionAppKey;
            }

            var capturedQlikTokens = await context.QlikCloudEnvironments
                .AsNoTracking()
                .Select(q => new { q.QlikCloudEnvironmentId, q.ApiToken })
                .ToArrayAsync(cancellationToken);

            foreach (var qlik in snapshot.QlikCloudEnvironments.Where(q => string.IsNullOrEmpty(q.ApiToken)))
            {
                qlik.ApiToken = capturedQlikTokens
                    .FirstOrDefault(q => q.QlikCloudEnvironmentId == qlik.QlikCloudEnvironmentId)
                    ?.ApiToken ?? "";
            }

            var capturedDatabricksWorkspaceTokens = await context.DatabricksWorkspaces
                .AsNoTracking()
                .Select(w => new { w.WorkspaceId, w.ApiToken })
                .ToArrayAsync(cancellationToken);

            foreach (var workspace in snapshot.DatabricksWorkspaces.Where(w => string.IsNullOrEmpty(w.ApiToken)))
            {
                workspace.ApiToken = capturedDatabricksWorkspaceTokens
                    .FirstOrDefault(w => w.WorkspaceId == workspace.WorkspaceId)
                    ?.ApiToken ?? "";
            }

            var capturedDbtAccountTokens = await context.DbtAccounts
                .AsNoTracking()
                .Select(a => new { a.DbtAccountId, a.ApiToken })
                .ToArrayAsync(cancellationToken);

            foreach (var account in snapshot.DbtAccounts.Where(a => string.IsNullOrEmpty(a.ApiToken)))
            {
                account.ApiToken = capturedDbtAccountTokens
                    .FirstOrDefault(a => a.DbtAccountId == account.DbtAccountId)
                    ?.ApiToken ?? "";
            }

            var capturedCredentials = await context.Credentials
                .AsNoTracking()
                .Select(c => new { c.CredentialId, c.Password })
                .ToArrayAsync(cancellationToken);

            foreach (var credential in snapshot.Credentials.Where(c => string.IsNullOrEmpty(c.Password)))
            {
                credential.Password = capturedCredentials
                    .FirstOrDefault(c => c.CredentialId == credential.CredentialId)
                    ?.Password;
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

            // Clear the change tracker so data from the db and snapshot do not interfere with eachother.
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

            var jobTagSubsToAdd = capturedSubscriptions
                .OfType<JobTagSubscription>()
                .Where(s => snapshot.Jobs.Any(j => j.JobId == s.JobId) && snapshot.Tags.Any(t => t.TagId == s.TagId && t.TagType == TagType.Step));

            var stepSubsToAdd = capturedSubscriptions
                .OfType<StepSubscription>()
                .Where(sub => snapshot.Jobs.Any(job => job.Steps.Any(step => step.StepId == sub.StepId)));

            var tagSubsToAdd = capturedSubscriptions
                .OfType<TagSubscription>()
                .Where(s => snapshot.Tags.Any(t => t.TagId == s.TagId && t.TagType == TagType.Step));

            context.JobSubscriptions.AddRange(jobSubsToAdd);
            context.JobTagSubscriptions.AddRange(jobTagSubsToAdd);
            context.StepSubscriptions.AddRange(stepSubsToAdd);
            context.TagSubscriptions.AddRange(tagSubsToAdd);

            await context.SaveChangesAsync(cancellationToken);

            // Add user job and data table authorizations that were captured at the beginning and where jobs and tables exist in the snapshot.

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reverting version");
            throw;
        }
    }
}