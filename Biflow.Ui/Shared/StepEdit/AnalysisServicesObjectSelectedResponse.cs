namespace Biflow.Ui.Shared.StepEdit;

public record AnalysisServicesObjectSelectedResponse(string ServerName, string ModelName, string? TableName = null, string? PartitionName = null)
{
    public void Deconstruct(out string modelName, out string? tableName, out string? partitionName)
    {
        (modelName, tableName, partitionName) = (ModelName, TableName, PartitionName);
    }
}