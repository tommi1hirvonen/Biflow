using System;
using System.ComponentModel.DataAnnotations;

namespace EtlManagerUi.Models
{
    public class StepExecutionParameter
    {
        [Key]
        public string StepExecutionParameterId { get; set; }

        public string StepExecutionId { get; set; }

        public string ParameterName { get; set; }

        public string ParameterValue { get; set; }

        public StepExecution StepExecution { get; set; }
    }
}
