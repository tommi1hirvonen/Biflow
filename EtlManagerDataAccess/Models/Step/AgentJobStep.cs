using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class AgentJobStep : Step
    {
        public AgentJobStep(string agentJobName) : base(StepType.AgentJob)
        {
            AgentJobName = agentJobName;
        }

        [Display(Name = "Agent job name")]
        [Required]
        [MinLength(1)]
        [MaxLength(128)]
        public string AgentJobName { get; set; }

        [Column("ConnectionId")]
        [Required]
        public Guid? ConnectionId { get; set; }

        public Connection? Connection { get; set; } = null!;
    }
}
