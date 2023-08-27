using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public class ExecutionBuilderTag : ITag
{
    private readonly Tag _tag;

    internal ExecutionBuilderTag(Tag tag)
    {
        _tag = tag;
    }

    public Guid TagId => _tag.TagId;

    public string TagName => _tag.TagName;

    public TagColor Color => _tag.Color;
}
