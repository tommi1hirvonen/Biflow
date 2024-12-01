using System.Text;

namespace Biflow.Core.Entities.Scd.Snowflake;

internal abstract class SnowflakeSyntaxProvider : ISqlSyntaxProvider
{
    // TODO: Handle potential quoted object identifiers
    // https://docs.snowflake.com/en/sql-reference/identifiers-syntax
    public static string QuoteName(string name) => name;

    public static string CurrentTimestamp => "CURRENT_TIMESTAMP";

    public static string DateTime => "TIMESTAMP_NTZ";

    public static string Boolean => "BOOLEAN";
    
    public static string Varchar(int length) => $"VARCHAR({length})";

    public static string Md5(IEnumerable<string> columns) =>
        $"UPPER(MD5(CONCAT({string.Join(", '|', ", columns)})))";

    public static string MaxDateTime => "CAST('9999-12-31' AS TIMESTAMP_NTZ)";

    public static string True => "1";

    public static bool SupportsDdlRollback => false;
    
    public static bool SupportsIndexes => false;

    public static string WithBlock(string block) => $"""
        BEGIN

        {block}

        END;
        """;
    
    public static string RollbackOnError(string block) => $"""
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

    public static string Ctas(
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

    public static string ScdUpdate(string source, string target, bool fullLoad,
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

    public static string AlterColumnDropNull(string table, IStructureColumn column) =>
        $"ALTER TABLE {table} ALTER COLUMN {QuoteName(column.ColumnName)} {column.DataType} NULL;";

    public static string AlterTableAddColumn(string table, IStructureColumn column, bool nullable) =>
        $"ALTER TABLE {table} ADD {QuoteName(column.ColumnName)} {column.DataType} {(nullable ? "NULL" : "NOT NULL")};";
}