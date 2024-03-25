namespace Biflow.Ui.Core;

public record TagProjection(Guid TagId, string TagName, TagColor Color) : ITag;