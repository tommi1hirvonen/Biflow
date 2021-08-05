using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class AppRegistration
    {
        [Key]
        [Required]
        [Display(Name = "App registration id")]
        public Guid AppRegistrationId { get; set; }

        [Required]
        [Display(Name = "Power BI Service name")]
        public string? AppRegistrationName { get; set; }

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

        [Required]
        [Display(Name = "Client secret")]
        public string? ClientSecret { get; set; }

    }
}
