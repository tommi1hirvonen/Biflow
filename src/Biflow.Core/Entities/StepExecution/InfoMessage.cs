using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[ComplexType]
public record InfoMessage(string Message, bool IsTruncated = false)
{
    public string Message { get; set; } = Message;
    
    public bool IsTruncated { get; set; } = IsTruncated;
}