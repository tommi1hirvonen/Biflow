﻿using Biflow.Core;
using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

public class SerializationTestsFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    public SqlConnectionBase[] SqlConnections { get; private set; } = [];
    public AnalysisServicesConnection[] AnalysisServicesConnections { get; private set; } = [];
    public Credential[] Credentials { get; private set; } = [];
    public Proxy[] Proxies { get; private set; } = [];
    public AzureCredential[] AzureCredentials { get; private set; } = [];
    public PipelineClient[] PipelineClients { get; private set; } = [];
    public FunctionApp[] FunctionApps { get; private set; } = [];
    public QlikCloudEnvironment[] QlikCloudClients { get; private set; } = [];
    public BlobStorageClient[] BlobStorageClients { get; private set; } = [];
    public DatabricksWorkspace[] DatabricksWorkspaces { get; private set; } = [];
    public DbtAccount[] DbtAccounts { get; private set; } = [];
    public ScdTable[] ScdTables { get; private set; } = [];
    
    public Job[] Jobs { get; private set; } = [];
    public Step[] Steps { get; private set; } = [];

    public Tag[] Tags { get; private set; } = [];
    public DataObject[] DataObjects {  get; private set; } = [];

    public MasterDataTable[] DataTables { get; private set; } = [];
    public MasterDataTableCategory[] DataTableCategories { get; private set; } = [];

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        SqlConnections = await context.SqlConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        AnalysisServicesConnections = await context.AnalysisServicesConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionId)
            .ToArrayAsync();
        Credentials = await context.Credentials
            .AsNoTracking()
            .OrderBy(c => c.Username)
            .ToArrayAsync();
        Proxies = await context.Proxies
            .AsNoTracking()
            .OrderBy(p => p.ProxyId)
            .ToArrayAsync();
        AzureCredentials = await context.AzureCredentials
            .AsNoTracking()
            .OrderBy(a => a.AzureCredentialId)
            .ToArrayAsync();
        PipelineClients = await context.PipelineClients
            .AsNoTracking()
            .OrderBy(p => p.PipelineClientId)
            .ToArrayAsync();
        FunctionApps = await context.FunctionApps
            .AsNoTracking()
            .OrderBy(f => f.FunctionAppId)
            .ToArrayAsync();
        QlikCloudClients = await context.QlikCloudEnvironments
            .AsNoTracking()
            .OrderBy(q => q.QlikCloudEnvironmentId)
            .ToArrayAsync();
        BlobStorageClients = await context.BlobStorageClients
            .AsNoTracking()
            .OrderBy(b => b.BlobStorageClientId)
            .ToArrayAsync();
        DatabricksWorkspaces = await context.DatabricksWorkspaces
            .AsNoTracking()
            .OrderBy(w => w.WorkspaceId)
            .ToArrayAsync();
        DbtAccounts = await context.DbtAccounts
            .AsNoTracking()
            .OrderBy(d => d.DbtAccountId)
            .ToArrayAsync();
        ScdTables = await context.ScdTables
            .AsNoTracking()
            .OrderBy(t => t.ScdTableId)
            .ToArrayAsync();


        Jobs = await context.Jobs
            .AsNoTracking()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .Include(j => j.Tags)
            .OrderBy(j => j.JobId)
            .ToArrayAsync();
        Steps = await context.Steps
            .AsNoTracking()
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters)
            .Include(s => s.Dependencies)
            .Include(s => s.DataObjects)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .OrderBy(s => s.JobId).ThenBy(s => s.StepId)
            .ToArrayAsync();
        var schedules = await context.Schedules
            .AsNoTracking()
            .Include(s => s.TagFilter)
            .Include(s => s.Tags)
            .OrderBy(s => s.JobId).ThenBy(s => s.ScheduleId)
            .ToArrayAsync();

        foreach (var job in Jobs)
        {
            job.Steps.AddRange(Steps.Where(s => s.JobId == job.JobId));
            job.Schedules.AddRange(schedules.Where(s => s.JobId == job.JobId));
        }

        Tags = await context.StepTags
            .AsNoTracking()
            .OrderBy(t => t.TagId)
            .ToArrayAsync();
        DataObjects = await context.DataObjects
            .AsNoTracking()
            .OrderBy(d => d.ObjectId)
            .ToArrayAsync();

        DataTables = await context.MasterDataTables
            .AsNoTracking()
            .Include(t => t.Lookups)
            .OrderBy(t => t.DataTableId)
            .ToArrayAsync();
        DataTableCategories = await context.MasterDataTableCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .ToArrayAsync();
    }
}