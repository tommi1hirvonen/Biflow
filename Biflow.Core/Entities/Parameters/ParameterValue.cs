using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class ParameterValue
{
    [JsonIgnore]
    public virtual object? Value
    {
        get => ValueType switch
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
        set
        {
            switch (value)
            {
                case bool b:
                    ValueBoolean = b;
                    ValueType = ParameterValueType.Boolean;
                    break;
                case DateTime b:
                    ValueDateTime = b;
                    ValueType = ParameterValueType.DateTime;
                    break;
                case decimal b:
                    ValueDecimal = b;
                    ValueType = ParameterValueType.Decimal;
                    break;
                case double b:
                    ValueDouble = b;
                    ValueType = ParameterValueType.Double;
                    break;
                case short b:
                    ValueInt16 = b;
                    ValueType = ParameterValueType.Int16;
                    break;
                case int b:
                    ValueInt32 = b;
                    ValueType = ParameterValueType.Int32;
                    break;
                case long b:
                    ValueInt64 = b;
                    ValueType = ParameterValueType.Int64;
                    break;
                case float b:
                    ValueSingle = b;
                    ValueType = ParameterValueType.Single;
                    break;
                case string b:
                    ValueString = b;
                    ValueType = ParameterValueType.String;
                    break;
            }
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public virtual ParameterValueType ValueType { get; set; } = ParameterValueType.String;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ValueBoolean { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ValueDateTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ValueDecimal { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ValueDouble { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public short? ValueInt16 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ValueInt32 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ValueInt64 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? ValueSingle { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValueString
    {
        get => _valueString;
        set
        {
            _valueString = string.IsNullOrEmpty(value) ? null : value;
        }
    }

    private string? _valueString;
}
