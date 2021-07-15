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

        [Display(Name = "Function app url")]
        public string? FunctionAppUrl { get; set; }

        [Display(Name = "Function app key")]
        public string? FunctionAppKey { get; set; }
    }
}
