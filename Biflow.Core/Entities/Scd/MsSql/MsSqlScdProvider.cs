using System.Text;

namespace Biflow.Core.Entities.Scd.MsSql;

internal class MsSqlScdProvider(ScdTable table, IColumnMetadataProvider columnProvider)
    : ScdProvider<MsSqlSyntaxProvider>(table, columnProvider)
{
    protected override string HashKeyColumn => "_HashKey";
    protected override string ValidFromColumn => "_ValidFrom";
    protected override string ValidUntilColumn => "_ValidUntil";
    protected override string IsCurrentColumn => "_IsCurrent";
    protected override string RecordHashColumn => "_RecordHash";
}