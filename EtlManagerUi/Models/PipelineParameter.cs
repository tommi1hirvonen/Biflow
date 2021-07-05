using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class PipelineParameter : ParameterBase
    {
        [Display(Name = "Step id")]
        [Column("StepId")]
        public Guid StepId { get; set; }

        public PipelineStep Step { get; set; } = null!;
    }

}
