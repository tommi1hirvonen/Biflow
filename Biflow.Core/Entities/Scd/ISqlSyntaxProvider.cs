namespace Biflow.Core.Entities.Scd;

internal interface ISqlSyntaxProvider
{
    public static abstract string QuoteName(string name);
    
    public static abstract string CurrentTimestamp { get; } 

    public static abstract string DateTime { get; }
    
    public static abstract string Boolean { get; }
    
    public static abstract string Varchar(int length);

    public static abstract string Md5(IEnumerable<string> columns);
    
    public static abstract string MaxDateTime { get; }
    
    public static abstract string True { get; }
    
    public static abstract bool SupportsDdlRollback { get; }
    
    public static abstract bool SupportsIndexes { get; }
    
    public static abstract string WithBlock(string block);
    
    public static abstract string RollbackOnError(string block);
    
    public static abstract string Ctas(
        string source, string target, IEnumerable<(string Expression, string ColumnName)> columns, bool distinct);

    public static abstract string ScdUpdate(string source, string target, bool fullLoad,
        string isCurrentColumn, string validUntilColumn, string hashKeyColumn, string recordHashColumn);
    
    public static abstract string AlterColumnDropNull(string table, IStructureColumn column);

    public static abstract string AlterTableAddColumn(string table, IStructureColumn column, bool nullable);
}