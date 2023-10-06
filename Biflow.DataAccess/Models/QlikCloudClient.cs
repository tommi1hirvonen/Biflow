using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("QlikCloudClient")]
public class QlikCloudClient
{
    [Key]
    public Guid QlikCloudClientId { get; private set; }

    [Required]
    public required string QlikCloudClientName { get; set; }

    [Required]
    public required string EnvironmentUrl { get; set; }

    [Required]
    public required string ApiToken { get; set; }
}
