using System.Data.Common;

namespace Biflow.Core;

public interface ISqlConnectionFactory
{
    public DbConnection Create();
}
