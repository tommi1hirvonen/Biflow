using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public enum StepType
{
    [Category("Integration", 1)]
    [Description("SQL Server Integration Services package")]
    Package,
    
    [Category("Integration", 1)]
    [Description("Azure Data Factory or Synapse pipeline")]
    Pipeline,
    
    [Category("Integration", 1)]
    [Description("Azure HTTP Function")]
    Function,
    
    [Category("Transformation", 2)]
    [Description("Custom SQL command (MS SQL or Snowflake)")]
    Sql,
    
    [Category("Reporting", 3)]
    [Description("SQL Server Analysis Services or Azure Analysis Services tabular model")]
    Tabular,
    
    [Category("Reporting", 3)]
    [Description("Power BI semantic model refresh")]
    Dataset,
    
    [Category("Reporting", 3)]
    [Description("Qlik Cloud app reload or automation run")]
    Qlik,
    
    [Category("Other", 4)]
    [Description("SQL Server Agent job")]
    AgentJob,
    
    [Category("Other", 4)]
    [Description("Executable")]
    Exe,
    
    [Category("Other", 4)]
    [Description("Send custom email")]
    Email,
    
    [Category("Other", 4)]
    [Description("Execute another job")]
    Job,

    [Category("Transformation", 2)]
    [Description("Run a Databricks job, notebook, Python file or pipeline")]
    Databricks,

    [Category("Transformation", 2)]
    [Description("Run a dbt Cloud job")]
    Dbt,
    
    [Category("Transformation", 2)]
    [Description("Run an SCD table load")]
    Scd
}