using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EtlManagerUi.Models
{
    public class Tag
    {
        [Key]
        public Guid TagId { get; set; }

        [Required]
        [MaxLength(250)]
        [MinLength(1)]
        public string? TagName { get; set; }

        public IList<Step> Steps { get; set; } = null!;
    }
}
