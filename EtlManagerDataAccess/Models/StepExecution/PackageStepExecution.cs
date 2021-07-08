using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class PackageStepExecution : ParameterizedStepExecution
    {
        public PackageStepExecution(string stepExecutionId, string stepName, string jobName, DateTime createdDateTime, string executionStatus)
            : base(stepExecutionId, stepName, jobName, createdDateTime, executionStatus)
        {
            StepExecutionId = stepExecutionId;
            StepName = stepName;
        }

        [Display(Name = "Package path")]
        public string? PackagePath { get; set; }

        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Display(Name = "Execute as login")]
        public string? ExecuteAsLogin { get; set; }

        public long? PackageOperationId { get; set; }
    }
}
