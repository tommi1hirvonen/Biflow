using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record FunctionStepExecution : StepExecution
    {
        public FunctionStepExecution(string stepName, Guid functionAppId, string functionName) : base(stepName)
        {
            FunctionAppId = functionAppId;
            FunctionName = functionName;
        }

        [Display(Name = "Function app id")]
        public Guid FunctionAppId { get; set; }
        
        [Display(Name = "Function name")]
        public string FunctionName { get; set; }

        [Display(Name = "Function input")]
        public string? FunctionInput { get; set; }

        [Column("TimeoutMinutes")]
        public int TimeoutMinutes { get; set; }
    }
}
