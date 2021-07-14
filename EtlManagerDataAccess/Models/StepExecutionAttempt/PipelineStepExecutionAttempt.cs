using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    public record PipelineStepExecutionAttempt : StepExecutionAttempt
    {
        public string? PipelineRunId { get; set; }
    }
}
