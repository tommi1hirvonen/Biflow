using System.Reflection;

namespace Biflow.DataAccess.Models;

public enum StepType
{
    [Category("Integration", 1)]
    [Description("SQL Server Integration Services package")]
    Package,
    
    [Category("Integration", 1)]
    [Description("Azure Data Factory or Synapse pipeline")]
    Pipeline,
    
    [Category("Integration", 1)]
    [Description("Azure Function")]
    Function,
    
    [Category("Transformation", 2)]
    [Description("Custom SQL command")]
    Sql,
    
    [Category("Reporting", 3)]
    [Description("SQL Server Analysis Services or Azure Analysis Services tabular model")]
    Tabular,
    
    [Category("Reporting", 3)]
    [Description("Power BI dataset refresh")]
    Dataset,
    
    [Category("Reporting", 3)]
    [Description("Qlik Cloud app reload")]
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
    Job
}

public static class StepTypeExtensions
{
    public static (string Name, int Ordinal)? GetCategory(this StepType value)
    {
        var name = Enum.GetName(value);
        if (name is null)
        {
            return null;
        }
        var category = typeof(StepType).GetField(name)?.GetCustomAttributes<CategoryAttribute>().FirstOrDefault();
        return category is not null
            ? (category.Name, category.Ordinal)
            : null;
    }

    public static string? GetDescription(this StepType value)
    {
        var name = Enum.GetName(value);
        if (name is null)
        {
            return null;
        }
        var description = typeof(StepType).GetField(name)?.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
        return description?.Text;
    }
}

[AttributeUsage(AttributeTargets.Field)]
file class CategoryAttribute(string name, int ordinal) : Attribute
{
    public string Name { get; } = name;

    public int Ordinal { get; } = ordinal;
}

[AttributeUsage(AttributeTargets.Field)]
file class DescriptionAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}