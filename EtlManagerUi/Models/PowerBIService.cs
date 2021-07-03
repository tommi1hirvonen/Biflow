using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class PowerBIService
    {
        [Key]
        [Required]
        [Display(Name = "Power BI Service id")]
        public Guid PowerBIServiceId { get; set; }

        [Required]
        [Display(Name = "Power BI Service name")]
        public string? PowerBIServiceName { get; set; }

        [Required]
        [Display(Name = "Tenant id")]
        [MaxLength(36)]
        [MinLength(36)]
        public string? TenantId { get; set; }

        [Required]
        [Display(Name = "Client id")]
        [MaxLength(36)]
        [MinLength(36)]
        public string? ClientId { get; set; }

        [NotMapped]
        [Required]
        [Display(Name = "Client secret")]
        public string? ClientSecret { get; set; }

        public IList<Step> Steps { get; set; } = null!;
    }
}
