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
    public record ExeStepExecutionAttempt : StepExecutionAttempt
    {
        [Column("InfoMessage")]
        public string? InfoMessage { get; set; }
    }
}
