using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasParameters
{
    public IList<ParameterBase> Parameters { get; }
}
