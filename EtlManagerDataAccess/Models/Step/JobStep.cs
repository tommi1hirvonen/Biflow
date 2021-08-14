using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record JobStep() : Step(StepType.Job)
    {
        [Display(Name = "Job to execute")]
        [Required]
        public Guid? JobToExecuteId { get; set; }

        [Display(Name = "Synchronized")]
        [Required]
        public bool JobExecuteSynchronized { get; set; }

        public Job JobToExecute { get; set; } = null!;
    }
}
