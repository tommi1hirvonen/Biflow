using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SerializationTests(SerializationTestsFixture fixture) : IClassFixture<SerializationTestsFixture>
{
    [Fact]
    public void Serialize_Connections()
    {
        var json = JsonSerializer.Serialize(fixture.SqlConnections, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<SqlConnectionBase[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.ConnectionId, Guid.Empty));
        Assert.All(items, x => Assert.DoesNotContain("password", x.ConnectionString, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serialize_AzureCredentials()
    {
        var json = JsonSerializer.Serialize(fixture.AzureCredentials, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<ServicePrincipalAzureCredential[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.AzureCredentialId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ClientSecret ?? ""));
    }

    [Fact]
    public void Serialize_PipelineClients()
    {
        var json = JsonSerializer.Serialize(fixture.PipelineClients, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<PipelineClient[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.PipelineClientId, Guid.Empty));
    }

    [Fact]
    public void Serialize_FunctionApps()
    {
        var json = JsonSerializer.Serialize(fixture.FunctionApps, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<FunctionApp[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.FunctionAppId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.FunctionAppKey ?? ""));
    }

    [Fact]
    public void Serialize_QlikCloudClients()
    {
        var json = JsonSerializer.Serialize(fixture.QlikCloudClients, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<QlikCloudEnvironment[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.QlikCloudEnvironmentId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ApiToken));
    }

    [Fact]
    public void Serialize_DbtAccounts()
    {
        var json = JsonSerializer.Serialize(fixture.DbtAccounts, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<DbtAccount[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.DbtAccountId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ApiToken));
    }

    [Fact]
    public void Serialize_BlobStorageClients()
    {
        var json = JsonSerializer.Serialize(fixture.BlobStorageClients, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<BlobStorageClient[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.BlobStorageClientId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ConnectionString ?? ""));
        Assert.All(items, x => Assert.DoesNotContain("sig=", x.StorageAccountUrl, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serialize_Jobs()
    {
        var json = JsonSerializer.Serialize(fixture.Jobs, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<Job[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.JobId, Guid.Empty));
        Assert.NotEmpty(items.SelectMany(x => x.Steps));
        Assert.NotEmpty(items.SelectMany(x => x.Schedules));
        Assert.NotEmpty(items.SelectMany(x => x.JobParameters));
        Assert.NotEmpty(items.SelectMany(x => x.JobConcurrencies));
        Assert.NotEmpty(items.SelectMany(x => x.Tags));
    }

    [Fact]
    public void Serialize_Steps()
    {
        var json = JsonSerializer.Serialize(fixture.Steps, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<Step[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.NotEmpty(items.SelectMany(s => s.ExecutionConditionParameters));
        Assert.All(items, x => Assert.NotEqual(x.StepId, Guid.Empty));
        Assert.All(items, x => Assert.NotEqual(x.JobId, Guid.Empty));
        var functionSteps = items.OfType<FunctionStep>();
        Assert.All(functionSteps, x => Assert.Empty(x.FunctionKey ?? ""));
        var sqlSteps = items.OfType<SqlStep>();
        Assert.NotEmpty(sqlSteps.SelectMany(s => s.StepParameters));
        Assert.NotEmpty(sqlSteps.SelectMany(s => s.StepParameters.Select(p => p.ExpressionParameters)));
    }

    [Fact]
    public void Serialize_Tags()
    {
        var json = JsonSerializer.Serialize(fixture.Tags, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<Tag[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.TagId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataObjects()
    {
        var json = JsonSerializer.Serialize(fixture.DataObjects, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<DataObject[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.ObjectId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataTables()
    {
        var json = JsonSerializer.Serialize(fixture.DataTables, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<MasterDataTable[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.DataTableId, Guid.Empty));
        Assert.All(items.SelectMany(i => i.Lookups), x => Assert.NotEqual(x.LookupId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataTableCategories()
    {
        var json = JsonSerializer.Serialize(fixture.DataTableCategories, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<MasterDataTableCategory[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.CategoryId, Guid.Empty));
    }

    [Fact]
    public void Serialize_Credentials()
    {
        var json = JsonSerializer.Serialize(fixture.Credentials, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        var items = JsonSerializer.Deserialize<Credential[]>(json, EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.CredentialId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.Password ?? ""));
    }

    [Fact]
    public void Serialize_Snapshot()
    {
        var snapshot = new EnvironmentSnapshot
        {
            SqlConnections = fixture.SqlConnections,
            AnalysisServicesConnections = fixture.AnalysisServicesConnections,
            Credentials = fixture.Credentials,
            AzureCredentials = fixture.AzureCredentials,
            PipelineClients = fixture.PipelineClients,
            FunctionApps = fixture.FunctionApps,
            QlikCloudEnvironments = fixture.QlikCloudClients,
            BlobStorageClients = fixture.BlobStorageClients,
            DatabricksWorkspaces = fixture.DatabricksWorkspaces,
            DbtAccounts = fixture.DbtAccounts,
            Jobs = fixture.Jobs,
            Tags = fixture.Tags,
            DataObjects = fixture.DataObjects,
            ScdTables = fixture.ScdTables,
            DataTables = fixture.DataTables,
            DataTableCategories = fixture.DataTableCategories
        };
        var json = snapshot.ToJson(preserveReferences: true);
        var items = EnvironmentSnapshot.FromJson(json, referencesPreserved: true);
        Assert.NotNull(items);
        Assert.NotEmpty(items.Jobs);
        var schedules = items.Jobs.SelectMany(j => j.Schedules).ToArray();
        Assert.NotEmpty(schedules);
    }
}
