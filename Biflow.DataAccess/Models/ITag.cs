namespace Biflow.DataAccess.Models;

public interface ITag
{
    public Guid TagId { get; }

    public string TagName { get; }

    public TagColor Color { get; }
}
