namespace Biflow.Core.Entities.Scd;

internal interface ISqlSyntaxProvider
{
    public string QuoteName(string name);
    public ISqlDatatypeProvider Datatypes { get; }  
    public ISqlFunctionProvider Functions { get; }
    public ISqlIndexProvider Indexes { get; }
    public bool SupportsDdlRollback { get; }
    public string WithBlock(string block);
    public string RollbackOnError(string block);
    public string Ctas(
        string source, string target, IEnumerable<(string Expression, string ColumnName)> columns, bool distinct);
    public string ScdUpdate(string source, string target, bool fullLoad,
        string isCurrentColumn, string validUntilColumn, string hashKeyColumn, string recordHashColumn);
    public string AlterColumnDropNull(string table, IStructureColumn column);
    public string AlterTableAddColumn(string table, IStructureColumn column, bool nullable);
}

internal interface ISqlDatatypeProvider
{
    public string DateTime { get; }
    public string Boolean { get; }
    public string Varchar(int length);
}

internal interface ISqlFunctionProvider
{
    public string CurrentTimestamp { get; }
    public string MaxDateTime { get; }
    public string True { get; }
    public string Md5(IEnumerable<string> columns);
}

internal interface ISqlIndexProvider
{
    public bool AreSupported { get; }
    public string ClusteredIndex(string table, string index, IEnumerable<(string ColumnName, bool Descending)> columns);
    public string NonClusteredIndex(string table, string index, IEnumerable<(string ColumnName, bool Descending)> columns);
}