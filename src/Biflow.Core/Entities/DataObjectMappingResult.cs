namespace Biflow.Core.Entities;

public class DataObjectMappingResult
{
    public bool IsNewAddition { get; set; }

    public bool IsUnreliableMapping { get; set; }

    public bool IsCandidateForRemoval { get; set; }
}