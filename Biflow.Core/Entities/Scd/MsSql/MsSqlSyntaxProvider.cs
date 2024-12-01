namespace Biflow.Core.Entities.Scd.MsSql;

internal sealed class MsSqlSyntaxProvider : ISqlSyntaxProvider
{
    public string QuoteColumn(string column) => column.QuoteName();
    public string QuoteTable(string? schema, string table) => 
        string.IsNullOrWhiteSpace(schema) 
            ? table.QuoteName() 
            : $"{schema.QuoteName()}.{table.QuoteName()}";
    
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
            $"CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT({string.Join(", '|', ", columns.Select(c => c.QuoteName()))})), 2)";
    }

    private class MsSqlIndexProvider : ISqlIndexProvider
    {
        public bool AreSupported => true;
        public string ClusteredIndex(
            string schema, string table, string index, IEnumerable<(string ColumnName, bool Descending)> columns)
        {
            var indexColumns = columns.Select(c => $"{c.ColumnName.QuoteName()} {(c.Descending ? "DESC" : "ASC")}");
            var indexColumnsDefinition = string.Join(", ", indexColumns);
            return $"""
                    CREATE CLUSTERED INDEX {index.QuoteName()} ON {schema.QuoteName()}.{table.QuoteName()} (
                    {indexColumnsDefinition}
                    );
                    """;
        }
        public string NonClusteredIndex(
            string schema, string table, string index, IEnumerable<(string ColumnName, bool Descending)> columns)
        {
            var indexColumns = columns.Select(c => $"{c.ColumnName.QuoteName()} {(c.Descending ? "DESC" : "ASC")}");
            var indexColumnsDefinition = string.Join(", ", indexColumns);
            return $"""
                    CREATE NONCLUSTERED INDEX {index.QuoteName()} ON {schema.QuoteName()}.{table.QuoteName()} (
                    {indexColumnsDefinition}
                    );
                    """;
        }
    }
    
    public ISqlDatatypeProvider Datatypes => new MsSqlDatatypeProvider();
    public ISqlFunctionProvider Functions => new MsSqlFunctionProvider();
    public ISqlIndexProvider Indexes => new MsSqlIndexProvider();

    public bool SupportsDdlRollback => true;

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

    public string CreateTable(string schema, string table, IEnumerable<IStructureColumn> columns)
    {
        var columnDefinitions = columns
            .Select(c => $"{c.ColumnName.QuoteName()} {c.DataType} {(c.IsNullable ? "NULL" : "NOT NULL")}");
        return $"""
            CREATE TABLE {schema.QuoteName()}.{table.QuoteName()} (
            {string.Join(",\n", columnDefinitions)}
            );
            """;
    }

    public string Ctas(
        string sourceSchema,
        string sourceTable,
        string? targetSchema,
        string targetTable,
        IEnumerable<(string? Expression, string ColumnName)> select,
        bool distinct)
    {
        var source = QuoteTable(sourceSchema, sourceTable);
        var target = QuoteTable(targetSchema, targetTable);
        var columns = select.Select(c =>
            c.Expression is null ? c.ColumnName.QuoteName() : $"{c.Expression} AS {c.ColumnName.QuoteName()}");
        return $"""
            DROP TABLE IF EXISTS {target};
            
            SELECT {(distinct ? "DISTINCT" : null)}
            {string.Join(",\n", columns)}
            INTO {target}
            FROM {source};
            """;
    }

    public string ScdUpdate(
        string? sourceSchema,
        string sourceTable,
        string targetSchema,
        string targetTable,
        bool fullLoad,
        string isCurrentColumn,
        string validUntilColumn,
        string hashKeyColumn,
        string recordHashColumn)
    {
        var source = QuoteTable(sourceSchema, sourceTable);
        var target = QuoteTable(targetSchema, targetTable);
        return fullLoad
            ? $"""
               UPDATE tgt
               SET {isCurrentColumn.QuoteName()} = 0, {validUntilColumn.QuoteName()} = GETDATE()
               FROM {target} AS tgt
               LEFT JOIN {source} AS src ON tgt.{hashKeyColumn.QuoteName()} = src.{hashKeyColumn.QuoteName()}
               WHERE tgt.{isCurrentColumn.QuoteName()} = 1 AND
                     (tgt.{recordHashColumn.QuoteName()} <> src.{recordHashColumn.QuoteName()} OR src.{recordHashColumn.QuoteName()} IS NULL);
                       
               """
            : $"""
               UPDATE tgt
               SET {isCurrentColumn.QuoteName()} = 0, {validUntilColumn.QuoteName()} = GETDATE()
               FROM {target} AS tgt
               INNER JOIN {source} AS src ON tgt.{hashKeyColumn.QuoteName()} = src.{hashKeyColumn.QuoteName()}
               WHERE tgt.{isCurrentColumn.QuoteName()} = 1 AND
                     tgt.{recordHashColumn.QuoteName()} <> src.{recordHashColumn.QuoteName()};
                     
               """;
    }

    public string AlterColumnDropNull(string schema, string table, IStructureColumn column) =>
        $"ALTER TABLE {schema.QuoteName()}.{table.QuoteName()} ALTER COLUMN {column.ColumnName.QuoteName()} {column.DataType} NULL;";

    public string AlterTableAddColumn(string schema, string table, IStructureColumn column, bool nullable) =>
        $"ALTER TABLE {schema.QuoteName()}.{table.QuoteName()} ADD {column.ColumnName.QuoteName()} {column.DataType} {(nullable ? "NULL" : "NOT NULL")};";
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