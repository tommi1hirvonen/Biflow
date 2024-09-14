using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasConnection
{
    public Guid ConnectionId { get; }

    public ConnectionBase? Connection { get; }
}