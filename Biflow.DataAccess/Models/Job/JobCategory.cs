using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("JobCategory")]
public class JobCategory
{
    [Key]
    [Column("JobCategoryId")]
    [JsonInclude]
    public Guid CategoryId { get; private set; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    [Column("JobCategoryName")]
    public string CategoryName { get; set; } = "";

    public ICollection<Job> Jobs { get; set; } = null!;
}
