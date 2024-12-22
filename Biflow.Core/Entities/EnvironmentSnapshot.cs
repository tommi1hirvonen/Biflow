using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Biflow.Core.Converters;

namespace Biflow.Core.Entities;

public class EnvironmentSnapshot
{
    public static readonly JsonSerializerOptions JsonSerializerOptionsPreserveReferences = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonModifiers.SensitiveModifier }
        }
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonModifiers.SensitiveModifier }
        }
    };

    public required Job[] Jobs { get; init; }
    public required Tag[] Tags { get; init; }
    public required DataObject[] DataObjects { get; init; }
    public required ScdTable[] ScdTables { get; init; }

    public required SqlConnectionBase[] SqlConnections { get; init; }
    public required AnalysisServicesConnection[] AnalysisServicesConnections { get; init; }
    public required Credential[] Credentials { get; init; }
    public required AzureCredential[] AzureCredentials { get; init; }
    public required PipelineClient[] PipelineClients { get; init; }
    public required FunctionApp[] FunctionApps { get; init; }
    public required QlikCloudEnvironment[] QlikCloudEnvironments { get; init; }
    public required DatabricksWorkspace[] DatabricksWorkspaces { get; init; }
    public required DbtAccount[] DbtAccounts { get; init; }
    public required BlobStorageClient[] BlobStorageClients { get; init; }

    public required MasterDataTableCategory[] DataTableCategories { get; init; }
    public required MasterDataTable[] DataTables { get; init; }

    public string ToJson(bool preserveReferences) =>
        JsonSerializer.Serialize(this,
            options: preserveReferences ? JsonSerializerOptionsPreserveReferences : JsonSerializerOptions);

    public static EnvironmentSnapshot? FromJson(string json, bool referencesPreserved) =>
        JsonSerializer.Deserialize<EnvironmentSnapshot>(json,
            options: referencesPreserved ? JsonSerializerOptionsPreserveReferences : JsonSerializerOptions);
}
