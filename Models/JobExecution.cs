using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class JobExecution : Execution
    {
        [Key]
        [Display(Name = "Execution id")]
        override public Guid ExecutionId { get; set; }
    }
}
