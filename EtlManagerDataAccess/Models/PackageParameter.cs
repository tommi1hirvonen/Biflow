using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class PackageParameter : ParameterBase
    {
        [Required]
        public string? ParameterLevel { get; set; }

        [Display(Name = "Step id")]
        [Column("StepId")]
        public Guid StepId { get; set; }

        public PackageStep Step { get; set; } = null!;
    }
}
