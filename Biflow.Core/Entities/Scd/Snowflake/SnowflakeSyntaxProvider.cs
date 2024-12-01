using System.Text;

namespace Biflow.Core.Entities.Scd.Snowflake;

internal sealed class SnowflakeSyntaxProvider : ISqlSyntaxProvider
{
    public string QuoteColumn(string column) => column.QuoteName();
    public string QuoteTable(string? schema, string table) => 
        string.IsNullOrWhiteSpace(schema) 
            ? table.QuoteName() 
            : $"{schema.QuoteName()}.{table.QuoteName()}";

    private class SnowflakeDatatypeProvider : ISqlDatatypeProvider
    {
        public string Varchar(int length) => $"VARCHAR({length})";
        public string DateTime => "TIMESTAMP_NTZ";
        public string Boolean => "BOOLEAN";
    }

    private class SnowflakeFunctionProvider : ISqlFunctionProvider
    {
        public string CurrentTimestamp => "CURRENT_TIMESTAMP";
        public string MaxDateTime => "CAST('9999-12-31' AS TIMESTAMP_NTZ)";
        public string True => "1";
        public string Md5(IEnumerable<string> columns) =>
            $"UPPER(MD5(CONCAT({string.Join(", '|', ", columns.Select(c => c.QuoteName()))})))";
    }

    private class SnowflakeIndexProvider : ISqlIndexProvider
    {
        public bool AreSupported => false;
        public string ClusteredIndex(
            string schema, string table, string index, IEnumerable<(string ColumnName, bool Descending)> columns) =>
            throw new NotSupportedException("Indexes are not supported on Snowflake.");

        public string NonClusteredIndex(
            string schema, string table, string index, IEnumerable<(string ColumnName, bool Descending)> columns) =>
            throw new NotSupportedException("Indexes are not supported on Snowflake.");
    }

    public ISqlDatatypeProvider Datatypes => new SnowflakeDatatypeProvider();
    public ISqlFunctionProvider Functions => new SnowflakeFunctionProvider();
    public ISqlIndexProvider Indexes => new SnowflakeIndexProvider();

    public bool SupportsDdlRollback => false;

    public string WithBlock(string block) => $"""
        BEGIN

        {block}

        END;
        """;
    
    public string RollbackOnError(string block) => $"""
        BEGIN

        BEGIN TRANSACTION;

        {block}

        COMMIT;

        EXCEPTION
            WHEN OTHER THEN

        ROLLBACK;
        RAISE;

        END;
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
        IEnumerable<(string Expression, string ColumnName)> select,
        bool distinct)
    {
        var source = QuoteTable(sourceSchema, sourceTable);
        var target = QuoteTable(targetSchema, targetTable);
        var columns = select.Select(c => $"{c.Expression} AS {c.ColumnName}");
        return $"""
            DROP TABLE IF EXISTS {target};
            
            CREATE TABLE {target} AS
            SELECT {(distinct ? "DISTINCT" : null)}
            {string.Join(",\n", columns)}
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
        var builder = new StringBuilder();
        if (fullLoad)
        {
            // Update removed records validity.
            builder.AppendLine($"""
                UPDATE {target} AS tgt
                SET {isCurrentColumn.QuoteName()} = 0, {validUntilColumn.QuoteName()} = CURRENT_TIMESTAMP
                WHERE tgt.{isCurrentColumn.QuoteName()} = 1 AND
                      NOT EXISTS (
                          SELECT *
                          FROM {source} AS src
                          WHERE tgt.{hashKeyColumn.QuoteName()} = src.{hashKeyColumn.QuoteName()}
                      );
                """);
        }
        
        // Update changed records validity.
        builder.AppendLine($"""
            UPDATE {target} AS tgt
            SET {isCurrentColumn.QuoteName()} = 0, {validUntilColumn.QuoteName()} = CURRENT_TIMESTAMP
            FROM {source} AS src
            WHERE tgt.{isCurrentColumn.QuoteName()} = 1 AND
                  tgt.{hashKeyColumn.QuoteName()} = src.{hashKeyColumn.QuoteName()} AND -- inner join
                  tgt.{recordHashColumn.QuoteName()} <> src.{recordHashColumn.QuoteName()};
            """);
        
        return builder.ToString();
    }

    public string AlterColumnDropNull(string schema, string table, IStructureColumn column) =>
        $"ALTER TABLE {schema.QuoteName()}.{table.QuoteName()} ALTER {column.ColumnName.QuoteName()} DROP NOT NULL;";

    public string AlterTableAddColumn(string schema, string table, IStructureColumn column, bool nullable) =>
        $"ALTER TABLE {schema.QuoteName()}.{table.QuoteName()} ADD {column.ColumnName.QuoteName()} {column.DataType} {(nullable ? "NULL" : "NOT NULL")};";
}

file static class Extensions
{
    // TODO: Handle potential quoted object identifiers
    // https://docs.snowflake.com/en/sql-reference/identifiers-syntax
    public static string QuoteName(this string name) => name;
}