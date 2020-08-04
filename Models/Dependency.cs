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
        public Guid DependencyId { get; set; }

        [Required]
        public Guid StepId { get; set; }

        public Step Step { get; set; }

        [Required]
        public Guid DependantOnStepId { get; set; }

        public Step DependantOnStep { get; set; }

        [Display(Name = "Strict dependency")]
        public bool StrictDependency { get; set; }
    }
}
