namespace Biflow.DataAccess.Models;

public interface IHasConnection<T> : IHasConnection
    where T : ConnectionInfoBase?
{
    public new T Connection { get; set; }

    ConnectionInfoBase? IHasConnection.Connection => Connection;
}

public interface IHasConnection
{
    public Guid ConnectionId { get; }

    public ConnectionInfoBase? Connection { get; }
}