using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models
{
    public class PackageParameter : ParameterBase
    {
        [Required]
        public ParameterLevel ParameterLevel { get; set; }

        [Display(Name = "Step id")]
        [Column("StepId")]
        public Guid StepId { get; set; }

        public PackageStep Step { get; set; } = null!;

        public Guid? JobParameterId { get; set; }

        public JobParameter? JobParameter { get; set; }
    }
}
