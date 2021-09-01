using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record SqlStepExecution : ParameterizedStepExecution
    {
        public SqlStepExecution(string stepName, string sqlStatement) : base(stepName, StepType.Sql)
        {
            SqlStatement = sqlStatement;
        }

        [Column("ConnectionId")]
        public Guid ConnectionId { get; set; }

        public Connection Connection { get; set; } = null!;

        [Display(Name = "SQL statement")]
        public string SqlStatement { get; set; }

        [Column("TimeoutMinutes")]
        public int TimeoutMinutes { get; set; }
    }
}
