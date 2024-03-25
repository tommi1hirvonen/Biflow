namespace Biflow.DataAccess;

public class ExecutionBuilderTag : ITag
{
    private readonly StepTag _tag;

    internal ExecutionBuilderTag(StepTag tag)
    {
        _tag = tag;
    }

    public Guid TagId => _tag.TagId;

    public string TagName => _tag.TagName;

    public TagColor Color => _tag.Color;
}
