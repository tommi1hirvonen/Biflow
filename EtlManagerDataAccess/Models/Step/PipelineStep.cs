using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record PipelineStep() : Step(StepType.Pipeline)
    {
        [Required]
        public Guid? DataFactoryId { get; set; }

        [MaxLength(250)]
        [Display(Name = "Pipeline name")]
        [Required]
        public string? PipelineName { get; set; }

        public IList<PipelineParameter> PipelineParameters { get; set; } = null!;

        public DataFactory? DataFactory { get; set; }
    }
}
