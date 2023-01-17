namespace Biflow.DataAccess.Models;

public interface IHasTimeout
{
    public double TimeoutMinutes { get; set; }
}
