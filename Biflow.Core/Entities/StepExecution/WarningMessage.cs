using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

public record WarningMessage(string Message, string? Exception);