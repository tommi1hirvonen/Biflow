namespace Biflow.Ui.Projections;

public record TagProjection(Guid TagId, string TagName, TagColor Color, int SortOrder) : ITag;