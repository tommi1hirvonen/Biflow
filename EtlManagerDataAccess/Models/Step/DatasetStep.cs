using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record DatasetStep : Step
    {
        [Required]
        public Guid? PowerBIServiceId { get; set; }

        [Display(Name = "Group id")]
        [MaxLength(36)]
        [MinLength(36)]
        [Required]
        public string? DatasetGroupId { get; set; }

        [Display(Name = "Dataset id")]
        [MaxLength(36)]
        [MinLength(36)]
        [Required]
        public string? DatasetId { get; set; }

        public PowerBIService? PowerBIService { get; set; }
    }
}
