using Biflow.Core.Entities;

namespace Biflow.Ui.TableEditor;

public class Lookup(
    MasterDataTableLookup dataTableLookup,
    Type displayValueDatatype,
    IEnumerable<(object? Value, object? DisplayValue)> values)
{
    public MasterDataTableLookup DataTableLookup { get; } = dataTableLookup;

    public Type DisplayValueDatatype { get; } = displayValueDatatype;

    public IEnumerable<(object? Value, object? DisplayValue)> Values { get; } = values.OrderBy(v => v.DisplayValue).ToArray();
}
