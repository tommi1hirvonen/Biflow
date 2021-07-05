using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class SqlStep : Step
    {
        [Display(Name = "SQL statement")]
        public string? SqlStatement { get; set; }

        [Column("ConnectionId")]
        public Guid? ConnectionId { get; set; }

        public Connection? Connection { get; set; }
    }
}
