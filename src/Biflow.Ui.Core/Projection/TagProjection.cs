namespace Biflow.Ui.Core;

public record TagProjection(Guid TagId, string TagName, TagColor Color, int SortOrder) : ITag;