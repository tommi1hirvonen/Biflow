using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class PipelineStepExecution : ParameterizedStepExecution
    {
        public PipelineStepExecution(string stepExecutionId, string stepName, string jobName, DateTime createdDateTime, string executionStatus)
            : base(stepExecutionId, stepName, jobName, createdDateTime, executionStatus)
        {
            StepExecutionId = stepExecutionId;
            StepName = stepName;
        }

        [Display(Name = "Pipeline name")]
        public string? PipelineName { get; set; }

        [Display(Name = "Data Factory id")]
        public Guid? DataFactoryId { get; set; }

        [Display(Name = "Pipeline run id")]
        public string? PipelineRunId { get; set; }
    }
}
