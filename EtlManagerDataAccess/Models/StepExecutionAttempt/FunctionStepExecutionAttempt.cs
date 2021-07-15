using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    public record FunctionStepExecutionAttempt : StepExecutionAttempt
    {
        [Display(Name = "Function instance id")]
        public string? FunctionInstanceId { get; set; }

        [Column("InfoMessage")]
        public string? InfoMessage { get; set; }
    }
}
