namespace Biflow.Core.Entities.Scd.Snowflake;

internal class SnowflakeScdProvider(ScdTable table, IColumnMetadataProvider columnProvider)
    : ScdProvider(table, columnProvider)
{
    protected override string HashKeyColumn => "_HASH_KEY";
    protected override string ValidFromColumn => "_VALID_FROM";
    protected override string ValidUntilColumn => "_VALID_UNTIL";
    protected override string IsCurrentColumn => "_IS_CURRENT";
    protected override string RecordHashColumn => "_RECORD_HASH";
    
    protected override ISqlSyntaxProvider SyntaxProvider { get; } = new SnowflakeSyntaxProvider();
}