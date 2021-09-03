using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public abstract class ParameterBase
    {
        [Key]
        [Required]
        [Display(Name = "Id")]
        public Guid ParameterId { get; set; }

        [Required]
        [MaxLength(128)]
        [Display(Name = "Name")]
        public string? ParameterName { get; set; }

        [Required]
        [Display(Name = "Value")]
        [Column(TypeName = "sql_variant")]
        public object? ParameterValue
        {
            get
            {
                return parameterValueField;
            }
            set
            {
                switch (value)
                {
                    case bool b:
                        ValueBoolean = b;
                        break;
                    case byte b:
                        ValueByte = b;
                        break;
                    case DateTime b:
                        ValueDateTime = b;
                        break;
                    case decimal b:
                        ValueDecimal = b;
                        break;
                    case double b:
                        ValueDouble = b;
                        break;
                    case short b:
                        ValueInt16 = b;
                        break;
                    case int b:
                        ValueInt32 = b;
                        break;
                    case long b:
                        ValueInt64 = b;
                        break;
                    case sbyte b:
                        ValueSByte = b;
                        break;
                    case float b:
                        ValueSingle = b;
                        break;
                    case string b:
                        ValueString = b;
                        break;
                    case uint b:
                        ValueUInt32 = b;
                        break;
                    case ulong b:
                        ValueUInt64 = b;
                        break;
                }
                parameterValueField = value;
            }
        }

        private object? parameterValueField;

        [Required]
        public ParameterType ParameterType { get; set; } = ParameterType.String;

        [NotMapped]
        public bool ValueBoolean { get; set; }
        [NotMapped]
        public byte ValueByte { get; set; }
        [NotMapped]
        public DateTime ValueDateTime { get; set; }
        [NotMapped]
        public decimal ValueDecimal { get; set; }
        [NotMapped]
        public double ValueDouble { get; set; }
        [NotMapped]
        public short ValueInt16 { get; set; }
        [NotMapped]
        public int ValueInt32 { get; set; }
        [NotMapped]
        public long ValueInt64 { get; set; }
        [NotMapped]
        public sbyte ValueSByte { get; set; }
        [NotMapped]
        public float ValueSingle { get; set; }
        [NotMapped]
        public string? ValueString { get; set; }
        [NotMapped]
        public uint ValueUInt32 { get; set; }
        [NotMapped]
        public ulong ValueUInt64 { get; set; }

        public void SetParameterValue()
        {
            ParameterValue = ParameterType switch
            {
                ParameterType.Boolean => ValueBoolean,
                ParameterType.Byte => ValueByte,
                ParameterType.DateTime => ValueDateTime,
                ParameterType.Decimal => ValueDecimal,
                ParameterType.Double => ValueDouble,
                ParameterType.Int16 => ValueInt16,
                ParameterType.Int32 => ValueInt32,
                ParameterType.Int64 => ValueInt64,
                ParameterType.SByte => ValueSByte,
                ParameterType.Single => ValueSingle,
                ParameterType.String => ValueString,
                ParameterType.UInt32 => ValueUInt32,
                ParameterType.UInt64 => ValueUInt64,
                _ => string.Empty
            };
        }
    }
}
