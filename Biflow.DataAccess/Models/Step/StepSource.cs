using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepSource")]
[PrimaryKey("StepId", "ObjectId")]
public class StepSource
{
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;

    public Guid ObjectId { get; set; }

    public DataObject DataObject { get; set; } = null!;
}
