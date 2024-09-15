using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class SerializationTests(SerializationTestsFixture fixture) : IClassFixture<SerializationTestsFixture>
{
    private static JsonSerializerOptions Options => EnvironmentSnapshot.JsonSerializerOptions;

    [Fact]
    public void Serialize_Connections()
    {
        var json = JsonSerializer.Serialize(fixture.Connections, Options);
        var items = JsonSerializer.Deserialize<ConnectionBase[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.ConnectionId, Guid.Empty));
        Assert.All(items, x => Assert.DoesNotContain("password", x.ConnectionString, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serialize_AppRegistrations()
    {
        var json = JsonSerializer.Serialize(fixture.AppRegistrations, Options);
        var items = JsonSerializer.Deserialize<AppRegistration[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.AppRegistrationId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ClientSecret ?? ""));
    }

    [Fact]
    public void Serialize_PipelineClients()
    {
        var json = JsonSerializer.Serialize(fixture.PipelineClients, Options);
        var items = JsonSerializer.Deserialize<PipelineClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.PipelineClientId, Guid.Empty));
    }

    [Fact]
    public void Serialize_FunctionApps()
    {
        var json = JsonSerializer.Serialize(fixture.FunctionApps, Options);
        var items = JsonSerializer.Deserialize<FunctionApp[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.FunctionAppId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.FunctionAppKey ?? ""));
    }

    [Fact]
    public void Serialize_QlikCloudClients()
    {
        var json = JsonSerializer.Serialize(fixture.QlikCloudClients, Options);
        var items = JsonSerializer.Deserialize<QlikCloudClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.QlikCloudClientId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ApiToken));
    }

    [Fact]
    public void Serialize_BlobStorageClients()
    {
        var json = JsonSerializer.Serialize(fixture.BlobStorageClients, Options);
        var items = JsonSerializer.Deserialize<BlobStorageClient[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.BlobStorageClientId, Guid.Empty));
        Assert.All(items, x => Assert.Empty(x.ConnectionString ?? ""));
        Assert.All(items, x => Assert.DoesNotContain("sig=", x.StorageAccountUrl, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serialize_Jobs()
    {
        var json = JsonSerializer.Serialize(fixture.Jobs, Options);
        var items = JsonSerializer.Deserialize<Job[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.JobId, Guid.Empty));
    }

    [Fact]
    public void Serialize_Steps()
    {
        var json = JsonSerializer.Serialize(fixture.Steps, Options);
        var items = JsonSerializer.Deserialize<Step[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.StepId, Guid.Empty));
        Assert.All(items, x => Assert.NotEqual(x.JobId, Guid.Empty));
        var functionSteps = items.OfType<FunctionStep>();
        Assert.All(functionSteps, x => Assert.Empty(x.FunctionKey ?? ""));
    }

    [Fact]
    public void Serialize_Tags()
    {
        var json = JsonSerializer.Serialize(fixture.Tags, Options);
        var items = JsonSerializer.Deserialize<StepTag[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.TagId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataObjects()
    {
        var json = JsonSerializer.Serialize(fixture.DataObjects, Options);
        var items = JsonSerializer.Deserialize<DataObject[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.ObjectId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataTables()
    {
        var json = JsonSerializer.Serialize(fixture.DataTables, Options);
        var items = JsonSerializer.Deserialize<MasterDataTable[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.DataTableId, Guid.Empty));
        Assert.All(items.SelectMany(i => i.Lookups), x => Assert.NotEqual(x.LookupId, Guid.Empty));
    }

    [Fact]
    public void Serialize_DataTableCategories()
    {
        var json = JsonSerializer.Serialize(fixture.DataTableCategories, Options);
        var items = JsonSerializer.Deserialize<MasterDataTableCategory[]>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotEqual(x.CategoryId, Guid.Empty));
    }

    [Fact]
    public void Serialize_Credentials()
    {
        var json = JsonSerializer.Serialize(fixture.Credentials, Options);
        var items = JsonSerializer.Deserialize<Credential[]>(json, Options);
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
            Connections = fixture.Connections,
            Credentials = fixture.Credentials,
            AppRegistrations = fixture.AppRegistrations,
            PipelineClients = fixture.PipelineClients,
            FunctionApps = fixture.FunctionApps,
            QlikCloudClients = fixture.QlikCloudClients,
            BlobStorageClients = fixture.BlobStorageClients,
            Jobs = fixture.Jobs,
            Tags = fixture.Tags,
            DataObjects = fixture.DataObjects,
            DataTables = fixture.DataTables,
            DataTableCategories = fixture.DataTableCategories
        };
        var json = JsonSerializer.Serialize(snapshot, Options);
        var items = JsonSerializer.Deserialize<EnvironmentSnapshot>(json, Options);
        Assert.NotNull(items);
        Assert.NotEmpty(items.Jobs);
        var schedules = items.Jobs.SelectMany(j => j.Schedules).ToArray();
        Assert.NotEmpty(schedules);
    }
}
