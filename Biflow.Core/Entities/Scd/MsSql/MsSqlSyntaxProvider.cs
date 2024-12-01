namespace Biflow.Core.Entities.Scd.MsSql;

internal sealed class MsSqlSyntaxProvider : ISqlSyntaxProvider
{
    public string QuoteName(string name) => name.QuoteName();
    
    private class MsSqlDatatypeProvider : ISqlDatatypeProvider
    {
        public string Varchar(int length) => $"VARCHAR({length})";
        public string DateTime => "DATETIME2(6)";
        public string Boolean => "BIT";
    }

    private class MsSqlFunctionProvider : ISqlFunctionProvider
    {
        public string CurrentTimestamp => "GETDATE()";
        public string MaxDateTime => "CONVERT(DATETIME2(6), '9999-12-31')";
        public string True => "1";
        public string Md5(IEnumerable<string> columns) =>
            $"CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT({string.Join(", '|', ", columns)})), 2)";
    }
    
    public ISqlDatatypeProvider Datatypes => new MsSqlDatatypeProvider();
    public ISqlFunctionProvider Functions => new MsSqlFunctionProvider();

    public bool SupportsDdlRollback => true;
    
    public bool SupportsIndexes => true;

    public string WithBlock(string block) => $"""
        BEGIN

        {block}

        END;
        """;
    
    public string RollbackOnError(string block) => $"""
        BEGIN TRY

        BEGIN TRANSACTION;

        {block}

        COMMIT TRANSACTION;

        END TRY
        BEGIN CATCH

        ROLLBACK TRANSACTION;
        THROW;

        END CATCH;
        """;

    public string Ctas(
        string source, string target, IEnumerable<(string Expression, string ColumnName)> select, bool distinct)
    {
        var columns = select.Select(c => $"{c.Expression} AS {c.ColumnName}");
        return $"""
            DROP TABLE IF EXISTS {target};
            
            SELECT {(distinct ? "DISTINCT" : null)}
            {string.Join(",\n", columns)}
            INTO {target}
            FROM {source};
            """;
    }

    public string ScdUpdate(string source, string target, bool fullLoad,
        string isCurrentColumn, string validUntilColumn, string hashKeyColumn, string recordHashColumn) =>
        fullLoad
            ? $"""
               UPDATE tgt
               SET {isCurrentColumn} = 0, {validUntilColumn} = GETDATE()
               FROM {target} AS tgt
               LEFT JOIN {source} AS src ON tgt.{hashKeyColumn} = src.{hashKeyColumn}
               WHERE tgt.{isCurrentColumn} = 1 AND
                     (tgt.{recordHashColumn} <> src.{recordHashColumn} OR src.{recordHashColumn} IS NULL);
                       
               """
            : $"""
               UPDATE tgt
               SET {isCurrentColumn} = 0, {validUntilColumn} = GETDATE()
               FROM {target} AS tgt
               INNER JOIN {source} AS src ON tgt.{hashKeyColumn} = src.{hashKeyColumn}
               WHERE tgt.{isCurrentColumn} = 1 AND
                     tgt.{recordHashColumn} <> src.{recordHashColumn};
                     
               """;

    public string AlterColumnDropNull(string table, IStructureColumn column) =>
        $"ALTER TABLE {table} ALTER COLUMN {QuoteName(column.ColumnName)} {column.DataType} NULL;";

    public string AlterTableAddColumn(string table, IStructureColumn column, bool nullable) =>
        $"ALTER TABLE {table} ADD {QuoteName(column.ColumnName)} {column.DataType} {(nullable ? "NULL" : "NOT NULL")};";
}

file static class Extensions
{
    private const int SysNameLength = 128;
    
    /// <summary>
    /// Returns a string with the delimiters added to make the input string
    /// a valid SQL Server delimited identifier. Unlike the T-SQL version,
    /// an ArgumentException is thrown instead of returning a null for
    /// invalid arguments.
    /// </summary>
    /// <param name="name">sysname, limited to 128 characters.</param>
    /// <param name="quoteCharacter">Can be a single quotation mark ( ' ), a
    /// left or right bracket ( [] ), or a double quotation mark ( " ).</param>
    /// <returns>An escaped identifier, no longer than 258 characters.</returns>
    public static string QuoteName(this string name, char quoteCharacter = '[') => (name, quoteCharacter) switch
    {
        ({ Length: > SysNameLength }, _) =>
            throw new ArgumentException($"{nameof(name)} is longer than {SysNameLength} characters"),
        (_, '\'') => $"'{name.Replace("'", "''")}'",
        (_, '"') => $"\"{name.Replace("\"", "\"\"")}\"",
        (_, '[' or ']') => $"[{name.Replace("]", "]]")}]",
        _ => throw new ArgumentException("quoteCharacter must be one of: ', \", [, or ]"),
    };
}