using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[NotMapped]
public record ErrorMessage(string Message, string? Exception);