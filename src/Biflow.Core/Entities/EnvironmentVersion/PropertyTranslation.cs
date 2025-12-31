using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class PropertyTranslation : IAuditable
{
    public Guid PropertyTranslationId { get; init; }
    
    [Required]
    [MaxLength(250)]
    public string PropertyTranslationName { get; set; } = "";
    
    /// <summary>
    /// Value used to sort the translations before applying them. Lower values are applied first.
    /// </summary>
    public int Order { get; set; }
    
    [Required]
    public string PropertyPath { get; set; } = "";
    
    /// <summary>
    /// String representation of the old value. If empty, the translation will be applied to any value.
    /// </summary>
    public string OldValue { get; set; } = "";
    
    /// <summary>
    /// If <see langword="true"/> and the type of the property value is <see cref="string"/>,
    /// the translation will only be applied if <see cref="OldValue"/> exactly matches the value of the property.
    /// </summary>
    public bool ExactMatch { get; set; }
    
    /// <summary>
    /// New value to replace the old value. The type of the value should match that of the original value.
    /// If the type of the value is <see cref="string"/> and <see cref="ExactMatch"/> is set to <see langword="false"/>,
    /// only the matching part of the string will be replaced.
    /// </summary>
    public ParameterValue NewValue { get; set; }
    
    public Guid PropertyTranslationSetId { get; init; }

    public PropertyTranslationSet PropertyTranslationSet { get; init; } = null!;
    
    public DateTimeOffset CreatedOn { get; set; }
    
    [MaxLength(250)]
    public string? CreatedBy { get; set; }
    
    public DateTimeOffset LastModifiedOn { get; set; }
    
    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static string ApplyTranslations(string json, IReadOnlyList<PropertyTranslation> propertyTranslations)
    {
        if (propertyTranslations.Count == 0)
            return json;
        
        var root = JsonNode.Parse(json);
        ArgumentNullException.ThrowIfNull(root);
        foreach (var translation in propertyTranslations.OrderBy(x => x.Order))
        {
            var segments = JsonPathParser.Parse(translation.PropertyPath, root);
            var evaluator = new JsonPathEvaluator(segments);
            foreach (var node in evaluator.Evaluate(root))
            {
                if (node is null)
                {
                    continue;
                }

                // For string values, handle partial replacements. For other types, replace the entire value.
                if (translation.NewValue.Value is string stringValue)
                {
                    var oldValue = node.ToJsonString();
                    // Trim quotes left by ToJsonString() from the old value if present
                    if (oldValue.StartsWith('"') && oldValue.EndsWith('"'))
                    {
                        oldValue = oldValue[1..^1];
                    }
                
                    // If exact match is enabled and the old value doesn't match the translation, skip the translation.
                    if (translation.ExactMatch && oldValue != translation.OldValue)
                    {
                        continue;
                    }
                    
                    var newValue = string.IsNullOrEmpty(translation.OldValue) // empty value => match any string
                        ? stringValue
                        : oldValue.Replace(translation.OldValue, stringValue);
                    node.ReplaceWith(JsonValue.Create(newValue));
                }
                else
                {
                    // For non-string types (number, bool, date, etc.), pass the typed value directly
                    // so JsonValue.Create preserves the correct JSON representation without quotes
                    node.ReplaceWith(JsonValue.Create(translation.NewValue.Value));
                }
            }
        }
        return root.ToJsonString(JsonSerializerOptions);
    }
}