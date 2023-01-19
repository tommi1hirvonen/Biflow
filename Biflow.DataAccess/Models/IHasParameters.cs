namespace Biflow.DataAccess.Models;

public interface IHasParameters
{
    public IList<ParameterBase> Parameters { get; }
}
