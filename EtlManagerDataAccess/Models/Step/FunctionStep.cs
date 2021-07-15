using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record FunctionStep : Step
    {
        [Required]
        public Guid? FunctionAppId { get; set; }

        [Display(Name = "Function name")]
        [MaxLength(250)]
        [Required]
        public string? FunctionName { get; set; }

        [Display(Name = "Function input")]
        public string? FunctionInput { get; set; }

        public FunctionApp FunctionApp { get; set; } = null!;
    }
}
