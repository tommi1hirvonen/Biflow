using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models
{
    public class StepExecutionParameter
    {
        [Key]
        public string? StepExecutionParameterId { get; set; }

        public string? StepExecutionId { get; set; }

        public string? ParameterName { get; set; }

        [Column(TypeName = "sql_variant")]
        public object? ParameterValue { get; set; }

        public string? ParameterType { get; set; }

        public string? ParameterLevel { get; set; }

        public StepExecution StepExecution { get; set; } = null!;
    }
}
