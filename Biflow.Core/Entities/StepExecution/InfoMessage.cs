using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[NotMapped]
public record InfoMessage(string Message);