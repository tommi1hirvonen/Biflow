using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class PipelineStepExecution : ParameterizedStepExecution
    {
        public PipelineStepExecution(string stepName) : base(stepName)
        {
        }

        [Display(Name = "Pipeline name")]
        public string? PipelineName { get; set; }

        [Display(Name = "Data Factory id")]
        public Guid? DataFactoryId { get; set; }
    }
}
