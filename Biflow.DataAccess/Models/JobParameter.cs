using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("JobParameter")]
public class JobParameter : ParameterBase
{
    [Display(Name = "Job")]
    [Column("JobId")]
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;
}
