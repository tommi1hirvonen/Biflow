using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class FunctionApp
    {
        [Required]
        [Display(Name = "Function app id")]
        public Guid FunctionAppId { get; set; }

        [Required]
        [Display(Name = "Function app name")]
        public string? FunctionAppName { get; set; }

        [Display(Name = "Function app key")]
        public string? FunctionAppKey
        {
            get => _functionAppKey;
            set => _functionAppKey = string.IsNullOrEmpty(value) ? null : value;
        }

        private string? _functionAppKey;

        [Required]
        [Display(Name = "Subscription id")]
        [MaxLength(36)]
        [MinLength(36)]
        public string? SubscriptionId { get; set; }

        [Required]
        [Display(Name = "Resource group name")]
        public string? ResourceGroupName { get; set; }

        [Required]
        [Display(Name = "Resource name")]
        public string? ResourceName { get; set; }

        [Required]
        [Display(Name = "App registration")]
        public Guid? AppRegistrationId { get; set; }

        public AppRegistration AppRegistration { get; set; } = null!;
    }
}
