using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class Parameter
    {
        [Key]
        [Required]
        [Display(Name = "Id")]
        public Guid ParameterId { get; set; }

        public Step Step { get; set; }

        [Display(Name = "Step id")]
        public Guid StepId { get; set; }

        [Required]
        [MaxLength(128)]
        [Display(Name = "Name")]
        public string ParameterName { get; set; }

        [Required]
        [Display(Name = "Value")]
        public string ParameterValue { get; set; }
    }
}
