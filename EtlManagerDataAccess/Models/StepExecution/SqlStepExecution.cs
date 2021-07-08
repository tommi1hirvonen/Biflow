using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class SqlStepExecution : StepExecution
    {
        public SqlStepExecution(string stepExecutionId, string stepName, string jobName, DateTime createdDateTime, string executionStatus)
            : base(stepExecutionId, stepName, jobName, createdDateTime, executionStatus)
        {
            StepExecutionId = stepExecutionId;
            StepName = stepName;
        }

        [Display(Name = "SQL statement")]
        public string? SqlStatement { get; set; }

        [Display(Name = "Info message")]
        public string? InfoMessage { get; set; }
    }
}
