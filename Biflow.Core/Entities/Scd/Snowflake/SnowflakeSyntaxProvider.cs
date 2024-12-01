using System.Text;

namespace Biflow.Core.Entities.Scd.Snowflake;

internal sealed class SnowflakeSyntaxProvider : ISqlSyntaxProvider
{
    // TODO: Handle potential quoted object identifiers
    // https://docs.snowflake.com/en/sql-reference/identifiers-syntax
    public string QuoteName(string name) => name;

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
            $"UPPER(MD5(CONCAT({string.Join(", '|', ", columns)})))";
    }

    public ISqlDatatypeProvider Datatypes => new SnowflakeDatatypeProvider();
    public ISqlFunctionProvider Functions => new SnowflakeFunctionProvider();

    public bool SupportsDdlRollback => false;
    
    public bool SupportsIndexes => false;

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

    public string Ctas(
        string source, string target, IEnumerable<(string Expression, string ColumnName)> select, bool distinct)
    {
        var columns = select.Select(c => $"{c.Expression} AS {c.ColumnName}");
        return $"""
            DROP TABLE IF EXISTS {target};
            
            CREATE TABLE {target} AS
            SELECT {(distinct ? "DISTINCT" : null)}
            {string.Join(",\n", columns)}
            FROM {source};
            """;
    }

    public string ScdUpdate(string source, string target, bool fullLoad,
        string isCurrentColumn, string validUntilColumn, string hashKeyColumn, string recordHashColumn)
    {
        var builder = new StringBuilder();
        if (fullLoad)
        {
            // Update removed records validity.
            builder.AppendLine($"""
                                UPDATE {target} AS tgt
                                SET {isCurrentColumn} = 0, {validUntilColumn} = CURRENT_TIMESTAMP
                                WHERE tgt.{isCurrentColumn} = 1 AND
                                      NOT EXISTS (
                                          SELECT *
                                          FROM {source} AS src
                                          WHERE tgt.{hashKeyColumn} = src.{hashKeyColumn}
                                      );
                                """);
        }
        
        // Update changed records validity.
        builder.AppendLine($"""
            UPDATE {target} AS tgt
            SET {isCurrentColumn} = 0, {validUntilColumn} = CURRENT_TIMESTAMP
            FROM {source} AS src
            WHERE tgt.{isCurrentColumn} = 1 AND
                  tgt.{hashKeyColumn} = src.{hashKeyColumn} AND -- inner join
                  tgt.{recordHashColumn} <> src.{recordHashColumn};
            """);
        
        return builder.ToString();
    }

    public string AlterColumnDropNull(string table, IStructureColumn column) =>
        $"ALTER TABLE {table} ALTER COLUMN {QuoteName(column.ColumnName)} {column.DataType} NULL;";

    public string AlterTableAddColumn(string table, IStructureColumn column, bool nullable) =>
        $"ALTER TABLE {table} ADD {QuoteName(column.ColumnName)} {column.DataType} {(nullable ? "NULL" : "NOT NULL")};";
}