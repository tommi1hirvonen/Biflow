using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record FunctionStep() : Step(StepType.Function)
    {
        [Required]
        public Guid? FunctionAppId { get; set; }

        [Display(Name = "Function url")]
        [MaxLength(1000)]
        [Required]
        public string? FunctionUrl { get; set; }

        [Display(Name = "Function input")]
        public string? FunctionInput
        {
            get => _functionInput;
            set => _functionInput = string.IsNullOrEmpty(value) ? null : value;
        }

        private string? _functionInput;

        [Display(Name = "Is durable")]
        public bool FunctionIsDurable { get; set; }

        [Display(Name = "Function key")]
        public string? FunctionKey { get; set; }

        public FunctionApp FunctionApp { get; set; } = null!;
    }
}
