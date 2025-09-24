using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public enum StepType
{
    [Category("Common", 1)]
    [Description("Custom SQL command (MS SQL or Snowflake)")]
    Sql,
    
    [Category("Common", 1)]
    [Description("Run an SCD table load")]
    Scd,
    
    [Category("Common", 1)]
    [Description("Execute another job")]
    Job,
    
    [Category("Fabric/Power BI", 2)]
    [Description("Fabric item job")]
    Fabric,
    
    [Category("Fabric/Power BI", 2)]
    [Description("Power BI/Fabric dataflow")]
    Dataflow,
    
    [Category("Fabric/Power BI", 2)]
    [Description("Power BI semantic model refresh")]
    Dataset,
    
    [Category("Azure", 3)]
    [Description("Azure Data Factory or Synapse pipeline")]
    Pipeline,
    
    [Category("Azure", 3)]
    [Description("Azure HTTP Function")]
    Function,
    
    [Category("Azure", 3)]
    [Description("Run a Databricks job, notebook, Python file or pipeline")]
    Databricks,

    [Category("Other", 4)]
    [Description("Run a dbt Cloud job")]
    Dbt,
    
    [Category("Other", 4)]
    [Description("Qlik Cloud app reload or automation run")]
    Qlik,
    
    [Category("Other", 4)]
    [Description("Executable")]
    Exe,
    
    [Category("Other", 4)]
    [Description("Send custom email")]
    Email,
    
    [Category("SQL Server", 5)]
    [Description("SQL Server Integration Services package")]
    Package,
    
    [Category("SQL Server", 5)]
    [Description("SQL Server Analysis Services or Azure Analysis Services tabular model")]
    Tabular,
    
    [Category("SQL Server", 5)]
    [Description("SQL Server Agent job")]
    AgentJob,
    
    [Category("Other", 4)]
    [Description("Send arbitrary HTTP request")]
    Http
}