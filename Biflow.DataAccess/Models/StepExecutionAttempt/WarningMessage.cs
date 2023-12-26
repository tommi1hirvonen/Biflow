using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[NotMapped]
public record WarningMessage(string Message, string? Exception);