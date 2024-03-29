using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public struct ParameterValue
{
    public ParameterValue(object? value, ParameterValueType? type = null)
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
            default:
                ValueType = type ?? ParameterValueType.Empty;
                break;
        }
    }

    [JsonIgnore]
    public readonly object? Value => ValueType switch
    {
        ParameterValueType.Empty => null,
        ParameterValueType.Boolean => ValueBoolean,
        ParameterValueType.DateTime => ValueDateTime,
        ParameterValueType.Decimal => ValueDecimal,
        ParameterValueType.Double => ValueDouble,
        ParameterValueType.Int16 => ValueInt16,
        ParameterValueType.Int32 => ValueInt32,
        ParameterValueType.Int64 => ValueInt64,
        ParameterValueType.Single => ValueSingle,
        ParameterValueType.String => ValueString,
        _ => null
    };

    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ParameterValueType ValueType { get; private set; } = ParameterValueType.Empty;

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private bool ValueBoolean { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private DateTime? ValueDateTime { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private decimal? ValueDecimal { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private double? ValueDouble { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private short? ValueInt16 { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private int? ValueInt32 { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private long? ValueInt64 { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private float? ValueSingle { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? ValueString
    {
        readonly get => _valueString;
        set
        {
            _valueString = string.IsNullOrEmpty(value) ? null : value;
        }
    }

    private string? _valueString;

    public readonly T? GetValueOrDefault<T>() => Value is T value ? value : default;

    public static ParameterValue DefaultValue(ParameterValueType type) => type switch
    {
        ParameterValueType.Empty => new(),
        ParameterValueType.Boolean => new(false),
        ParameterValueType.DateTime => new(DateTime.MinValue),
        ParameterValueType.Decimal => new(0.0m),
        ParameterValueType.Double => new(0.0d),
        ParameterValueType.Int16 => new((short)0),
        ParameterValueType.Int32 => new(0),
        ParameterValueType.Int64 => new(0L),
        ParameterValueType.Single => new(0f),
        ParameterValueType.String => new(""),
        _ => new()
    };

    public static bool TryCreate(ParameterValueType type, object? value, out ParameterValue result)
    {
        (result, var success) = (type, value) switch
        {
            (ParameterValueType.Boolean, bool b) => (new(b), true),
            (ParameterValueType.DateTime, DateTime d) => (new(d), true),
            (ParameterValueType.Decimal, decimal d) => (new(d), true),
            (ParameterValueType.Double, double d) => (new(d), true),
            (ParameterValueType.Int16, short s) => (new(s), true),
            (ParameterValueType.Int32, int i) => (new(i), true),
            (ParameterValueType.Int64, long l) => (new(l), true),
            (ParameterValueType.Single, float f) => (new(f), true),
            (ParameterValueType.String, string s) => (new(s), true),
            _ => (DefaultValue(type), false)
        };
        return success;
    }
}
