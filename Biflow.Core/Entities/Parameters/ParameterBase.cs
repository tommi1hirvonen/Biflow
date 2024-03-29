using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public abstract class ParameterBase
{
    [Required]
    [Display(Name = "Id")]
    [JsonInclude]
    public Guid ParameterId { get; protected set; }

    [Required]
    [MaxLength(128)]
    [Display(Name = "Name")]
    public string ParameterName { get; set; } = string.Empty;

    public virtual ParameterValue ParameterValue { get; set; } = new() { ValueType = ParameterValueType.String };

    [JsonIgnore]
    public virtual string DisplayName => ParameterName;

    [JsonIgnore]
    public virtual string DisplayValue => ParameterValue.Value?.ToString() ?? "null";

    [JsonIgnore]
    public virtual string DisplayValueType => ParameterValue.ValueType.ToString();

    [JsonIgnore]
    public virtual string DisplaySummary => DisplayValue switch
    {
        { Length: < 45 } => $"{DisplayName} ({DisplayValueType} = {DisplayValue})",
        _ => $"{DisplayName} ({DisplayValueType} = {DisplayValue[..40]}...)"
    };
}
