namespace Biflow.DataAccess.Models;

public interface IAuditable
{
    public DateTimeOffset CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    public string? LastModifiedBy { get; set;  }
}
