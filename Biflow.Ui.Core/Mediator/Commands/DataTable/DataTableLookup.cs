namespace Biflow.Ui.Core;

public record DataTableLookup(
    Guid? LookupId,
    string ColumnName,
    Guid LookupDataTableId,
    string LookupValueColumn,
    string LookupDescriptionColumn,
    LookupDisplayType LookupDisplayType);