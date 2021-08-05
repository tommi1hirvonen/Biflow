using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models
{
    public class DataFactory
    {
        [Key]
        [Required]
        [Display(Name = "Data Factory id")]
        public Guid DataFactoryId { get; set; }
        
        [Required]
        [Display(Name = "Data Factory name")]
        public string? DataFactoryName { get; set; }
       
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
