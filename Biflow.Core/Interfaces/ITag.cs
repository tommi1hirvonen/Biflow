using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface ITag
{
    public Guid TagId { get; }

    public string TagName { get; }

    public TagColor Color { get; }
}
