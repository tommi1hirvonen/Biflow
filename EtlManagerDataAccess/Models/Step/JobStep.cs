using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class JobStep : Step
    {
        [Display(Name = "Job to execute")]
        public Guid? JobToExecuteId { get; set; }

        [Display(Name = "Synchronized")]
        public bool JobExecuteSynchronized { get; set; }
    }
}
