using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[ComplexType]
public record InfoMessage(string Message)
{
    public string Message { get; set; } = Message;
}