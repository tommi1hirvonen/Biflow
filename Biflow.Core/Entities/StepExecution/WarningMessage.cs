using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[ComplexType]
public record WarningMessage(string Message, string? Exception);