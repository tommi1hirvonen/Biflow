using Biflow.Core.Attributes;
using Biflow.Core.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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
            Modifiers = { SensitiveModifier }
        }
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { SensitiveModifier }
        }
    };

    public required Job[] Jobs { get; init; }
    public required Tag[] Tags { get; init; }
    public required DataObject[] DataObjects { get; init; }

    public required ConnectionBase[] Connections { get; init; }
    public required Credential[] Credentials { get; init; }
    public required AppRegistration[] AppRegistrations { get; init; }
    public required PipelineClient[] PipelineClients { get; init; }
    public required FunctionApp[] FunctionApps { get; init; }
    public required QlikCloudEnvironment[] QlikCloudEnvironments { get; init; }
    public required DatabricksWorkspace[] DatabricksWorkspaces { get; init; }
    public required BlobStorageClient[] BlobStorageClients { get; init; }

    public required MasterDataTableCategory[] DataTableCategories { get; init; }
    public required MasterDataTable[] DataTables { get; init; }

    public string ToJson(bool preserveReferences) =>
        JsonSerializer.Serialize(this, preserveReferences ? JsonSerializerOptionsPreserveReferences : JsonSerializerOptions);

    public static EnvironmentSnapshot? FromJson(string json, bool referencesPreserved) =>
        JsonSerializer.Deserialize<EnvironmentSnapshot>(json, referencesPreserved ? JsonSerializerOptionsPreserveReferences : JsonSerializerOptions);

    private static void SensitiveModifier(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties.Where(p => p.PropertyType == typeof(string)))
        {
            var attributes = property.AttributeProvider
                ?.GetCustomAttributes(typeof(JsonSensitiveAttribute), true)
                ?? [];

            if (attributes.Length == 0)
            {
                continue;
            }

            var attribute = (JsonSensitiveAttribute)attributes[0];
            property.CustomConverter = new SensitiveStringConverter(attribute);
        }
    }
}
