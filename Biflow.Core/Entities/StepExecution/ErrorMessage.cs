using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[NotMapped]
public record ErrorMessage(string Message, string? Exception);