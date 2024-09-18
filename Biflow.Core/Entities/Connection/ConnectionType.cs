
using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public enum ConnectionType
{
    [Category("MS SQL", 1)]
    [Description("SQL Server, Azure SQL, Azure Synapse")]
    Sql,

    [Category("Analysis Services", 3)]
    [Description("SQL Server Analysis Services")]
    AnalysisServices,

    [Category("Snowflake", 2)]
    [Description("Snowflake")]
    Snowflake
}
