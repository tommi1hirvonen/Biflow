using Biflow.DataAccess.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Biflow.DataAccess;

public class EnvironmentSnapshot
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { SensitiveModifier }
        }
    };

    public required ConnectionInfoBase[] Connections { get; init; }
    
    public required AppRegistration[] AppRegistrations { get; init; }
    
    public required PipelineClient[] PipelineClients { get; init; }
    
    public required FunctionApp[] FunctionApps { get; init; }
    
    public required QlikCloudClient[] QlikCloudClients { get; init; }
    
    public required BlobStorageClient[] BlobStorageClients { get; init; }
    
    public required Job[] Jobs { get; init; }
    
    public required JobCategory[] JobCategories { get; init; }
    
    public required Step[] Steps { get; init; }
    
    public required Tag[] Tags { get; init; }
    
    public required DataObject[] DataObjects { get; init; }
    
    public required MasterDataTable[] DataTables { get; init; }
    
    public required MasterDataTableCategory[] DataTableCategories { get; init; }

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
