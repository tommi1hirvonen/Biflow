using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[NotMapped]
public record InfoMessage(string Message);