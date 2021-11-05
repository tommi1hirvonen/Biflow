using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class SqlStep : ParameterizedStep
    {
        public SqlStep() : base(StepType.Sql) { }

        [Display(Name = "SQL statement")]
        [Required]
        public string? SqlStatement { get; set; }

        [Column("ConnectionId")]
        [Required]
        public Guid? ConnectionId { get; set; }

        [Display(Name = "Result capture job parameter")]
        public Guid? ResultCaptureJobParameterId { get; set; }

        public SqlConnectionInfo? Connection { get; set; }
    }
}
