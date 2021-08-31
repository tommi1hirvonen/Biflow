using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class JobParameter : ParameterBase
    {
        [Display(Name = "Job")]
        [Column("JobId")]
        public Guid JobId { get; set; }

        public Job Job { get; set; } = null!;
    }
}
