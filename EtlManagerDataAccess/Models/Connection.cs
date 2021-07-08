using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public class Connection
    {
        [Key]
        [Display(Name = "Connection id")]
        public Guid ConnectionId { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Connection name")]
        public string? ConnectionName { get; set; }

        [Required]
        [Display(Name = "Connection string")]
        public string? ConnectionString { get; set; }

        [Required]
        [Display(Name = "Sensitive")]
        public bool IsSensitive { get; set; }

        [Display(Name = "Execute packages as login")]
        public string? ExecutePackagesAsLogin { get; set; }

    }
}
