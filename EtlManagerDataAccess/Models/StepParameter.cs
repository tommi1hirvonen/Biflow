using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class StepParameter : ParameterBase
    {
        [Display(Name = "Step id")]
        [Column("StepId")]
        public Guid StepId { get; set; }

        public ParameterizedStep Step { get; set; } = null!;
    }

}
