using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasSqlConnection
{
    public Guid ConnectionId { get; }

    public SqlConnectionBase? Connection { get; }
}