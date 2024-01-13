using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[NotMapped]
public record WarningMessage(string Message, string? Exception);