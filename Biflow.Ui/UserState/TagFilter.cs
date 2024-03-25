namespace Biflow.Ui.StateManagement;

public record TagFilter(Guid TagId, string TagName, TagColor Color) : ITag;