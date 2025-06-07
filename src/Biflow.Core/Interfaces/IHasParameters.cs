using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasParameters
{
    public IEnumerable<ParameterBase> Parameters { get; }
}
