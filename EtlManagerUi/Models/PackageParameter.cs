using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class PackageParameter : ParameterBase
    {
        [Required]
        public string ParameterLevel { get; set; }
    }
}
