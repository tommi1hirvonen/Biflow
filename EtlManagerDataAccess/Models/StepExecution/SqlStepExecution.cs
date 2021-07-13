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
        public SqlStepExecution(string stepName) : base(stepName)
        {
        }

        [Display(Name = "SQL statement")]
        public string? SqlStatement { get; set; }
    }
}
