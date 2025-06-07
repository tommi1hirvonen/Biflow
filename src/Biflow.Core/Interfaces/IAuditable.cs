namespace Biflow.Core.Interfaces;

public interface IAuditable
{
    public DateTimeOffset CreatedOn { set; }

    public string? CreatedBy { set; }

    public DateTimeOffset LastModifiedOn { set; }

    public string? LastModifiedBy { set;  }
}
