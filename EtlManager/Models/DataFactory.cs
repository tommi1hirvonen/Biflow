using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace EtlManager.Models
{
    public class DataFactory
    {
        [Key]
        [Required]
        [Display(Name = "Data Factory id")]
        public Guid DataFactoryId { get; set; }
        
        [Required]
        [Display(Name = "Data Factory name")]
        public string DataFactoryName { get; set; }
        
        [Required]
        [Display(Name = "Tenant id")]
        public Guid TenantId { get; set; }
       
        [Required]
        [Display(Name = "Subscription id")]
        public Guid SubscriptionId { get; set; }
       
        [Required]
        [Display(Name = "Client id")]
        public Guid ClientId { get; set; }
        
        [Required]
        [Display(Name = "Client secret")]
        public string ClientSecret { get; set; }
        
        [Required]
        [Display(Name = "Resource group name")]
        public string ResourceGroupName { get; set; }
        
        [Required]
        [Display(Name = "Resource name")]
        public string ResourceName { get; set; }

        public IList<Step> Steps { get; set; }
    }
}
