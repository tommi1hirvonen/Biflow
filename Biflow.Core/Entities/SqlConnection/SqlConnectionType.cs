
using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public enum SqlConnectionType
{
    [Category("MS SQL", 1)]
    [Description("SQL Server, Azure SQL, Azure Synapse")]
    MsSql,

    [Category("Snowflake", 2)]
    [Description("Snowflake")]
    Snowflake
}
