using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    [Display(Name = "Value")]
    [Column(TypeName = "sql_variant")]
    [JsonIgnore]
    public virtual object? ParameterValue
    {
        get => _parameterValue;
        set
        {
            switch (value)
            {
                case bool b:
                    _valueBoolean = b;
                    break;
                case DateTime b:
                    _valueDateTime = b;
                    break;
                case decimal b:
                    _valueDecimal = b;
                    break;
                case double b:
                    _valueDouble = b;
                    break;
                case short b:
                    _valueInt16 = b;
                    break;
                case int b:
                    _valueInt32 = b;
                    break;
                case long b:
                    _valueInt64 = b;
                    break;
                case float b:
                    _valueSingle = b;
                    break;
                case string b:
                    _valueString = b;
                    break;
            }
            _parameterValue = value;
        }
    }

    private object? _parameterValue = string.Empty;

    public virtual ParameterValueType ParameterValueType
    {
        get => _parameterValueType;
        set
        {
            if (_parameterValueType == value) return;
            _parameterValueType = value;
            _parameterValue = value switch
            {
                ParameterValueType.Boolean => ValueBoolean,
                ParameterValueType.DateTime => ValueDateTime,
                ParameterValueType.Decimal => ValueDecimal,
                ParameterValueType.Double => ValueDouble,
                ParameterValueType.Int16 => ValueInt16,
                ParameterValueType.Int32 => ValueInt32,
                ParameterValueType.Int64 => ValueInt64,
                ParameterValueType.Single => ValueSingle,
                ParameterValueType.String => ValueString,
                _ => string.Empty
            };
        }
    }

    private ParameterValueType _parameterValueType = ParameterValueType.String;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ValueBoolean
    {
        get => _valueBoolean;
        set
        {
            _valueBoolean = value;
            _parameterValue = value;
        }
    }

    private bool _valueBoolean;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ValueDateTime
    {
        get => _valueDateTime;
        set
        {
            _valueDateTime = value;
            _parameterValue = value;
        }
    }

    private DateTime? _valueDateTime;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ValueDecimal
    {
        get => _valueDecimal;
        set
        {
            _valueDecimal = value;
            _parameterValue = value;
        }
    }

    private decimal? _valueDecimal;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ValueDouble
    {
        get => _valueDouble;
        set
        {
            _valueDouble = value;
            _parameterValue = value;
        }
    }

    private double? _valueDouble;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public short? ValueInt16
    {
        get => _valueInt16;
        set
        {
            _valueInt16 = value;
            _parameterValue = value;
        }
    }

    private short? _valueInt16;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ValueInt32
    {
        get => _valueInt32;
        set
        {
            _valueInt32 = value;
            _parameterValue = value;
        }
    }

    private int? _valueInt32;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ValueInt64
    {
        get => _valueInt64;
        set
        {
            _valueInt64 = value;
            _parameterValue = value;
        }
    }

    private long? _valueInt64;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? ValueSingle
    {
        get => _valueSingle;
        set
        {
            _valueSingle = value;
            _parameterValue = value;
        }
    }

    private float? _valueSingle;

    [NotMapped]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValueString
    {
        get => _valueString;
        set
        {
            _valueString = string.IsNullOrEmpty(value) ? null : value;
            _parameterValue = _valueString;
        }
    }

    private string? _valueString;

    [JsonIgnore]
    public virtual string DisplayName => ParameterName;

    [JsonIgnore]
    public virtual string DisplayValue => ParameterValue?.ToString() ?? "null";

    [JsonIgnore]
    public virtual string DisplayValueType => ParameterValueType.ToString();

    [JsonIgnore]
    public virtual string DisplaySummary => DisplayValue switch
    {
        { Length: <45 } => $"{DisplayName} ({DisplayValueType} = {DisplayValue})",
        _ => $"{DisplayName} ({DisplayValueType} = {DisplayValue[..40]}...)"
    };
}
