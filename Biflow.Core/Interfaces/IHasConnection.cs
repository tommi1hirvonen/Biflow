using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasConnection<T> : IHasConnection
    where T : ConnectionBase?
{
    public new T Connection { get; set; }

    ConnectionBase? IHasConnection.Connection => Connection;
}

public interface IHasConnection
{
    public Guid ConnectionId { get; }

    public ConnectionBase? Connection { get; }
}