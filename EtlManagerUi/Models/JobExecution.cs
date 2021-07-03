using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class JobExecution : Execution
    {

        public JobExecution(string jobName, DateTime createdDateTime, string executionStatus)
            : base(jobName, createdDateTime, executionStatus)
        {

        }

        [Key]
        [Display(Name = "Execution id")]
        override public Guid ExecutionId { get; set; }

        public int NumberOfSteps { get; set; }

        [DisplayFormat(DataFormatString = "{0:N0}%")]
        public decimal SuccessPercent { get; set; }
    }
}
