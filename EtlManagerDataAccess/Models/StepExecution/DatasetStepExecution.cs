using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class DatasetStepExecution : StepExecution
    {
        public DatasetStepExecution(string stepExecutionId, string stepName, string jobName, DateTime createdDateTime, string executionStatus)
            : base(stepExecutionId, stepName, jobName, createdDateTime, executionStatus)
        {
            StepExecutionId = stepExecutionId;
            StepName = stepName;
        }

        [Display(Name = "Power BI Service id")]
        public Guid? PowerBIServiceId { get; set; }
        
        [Display(Name = "Group id")]
        public string? DatasetGroupId { get; set; }

        [Display(Name = "Dataset id")]
        public string? DatasetId { get; set; }
    }
}
