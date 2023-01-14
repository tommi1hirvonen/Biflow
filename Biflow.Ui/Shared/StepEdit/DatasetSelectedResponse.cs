namespace Biflow.Ui.Shared.StepEdit;

public record DatasetSelectedResponse(string GroupId, string DatasetId)
{
    public void Desconstruct(out string groupId, out string datasetId)
    {
        (groupId, datasetId) = (GroupId, DatasetId);
    }
}