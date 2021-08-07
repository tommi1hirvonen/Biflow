using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record PipelineStepExecution : ParameterizedStepExecution
    {
        public PipelineStepExecution(string stepName, string pipelineName) : base(stepName)
        {
            PipelineName = pipelineName;
        }

        [Display(Name = "Pipeline name")]
        public string PipelineName { get; set; }

        [Display(Name = "Data Factory id")]
        public Guid DataFactoryId { get; set; }

        public DataFactory DataFactory { get; set; } = null!;

        [Column("TimeoutMinutes")]
        public int TimeoutMinutes { get; set; }
    }
}
