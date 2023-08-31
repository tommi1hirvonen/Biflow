namespace Biflow.Ui.Shared.StepEdit;

public record DatasetSelectedResponse(string GroupId, string GroupName, string DatasetId, string DatasetName)
{
    public void Desconstruct(out string groupId, out string groupName, out string datasetId, out string datasetName)
    {
        (groupId, groupName, datasetId, datasetName) = (GroupId, GroupName, DatasetId, DatasetName);
    }
}