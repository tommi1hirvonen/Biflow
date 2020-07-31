using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class Dependency
    {
        [Key]
        public string DependencyId { get; set; }

        public Guid JobId { get; set; }

        [Display(Name = "Step")]
        public string StepName { get; set; }

        [Display(Name = "Dependant On")]
        public string DependantOnStepName { get; set; }

        [Display(Name = "Strict Dependency")]
        public bool StrictDependency { get; set; }
    }
}
