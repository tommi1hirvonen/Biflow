namespace Biflow.DataAccess.Models;

public interface ISoftDeletable
{
    public DateTimeOffset? DeletedOn { get; set; }
}
