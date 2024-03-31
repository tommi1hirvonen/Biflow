namespace Biflow.Ui.TableEditor;

public class Lookup(
    MasterDataTableLookup dataTableLookup,
    Type displayValueDatatype,
    IEnumerable<LookupValue> values)
{
    public MasterDataTableLookup DataTableLookup { get; } = dataTableLookup;

    public Type DisplayValueDatatype { get; } = displayValueDatatype;

    public IEnumerable<LookupValue> Values { get; } = values.OrderBy(v => v.DisplayValue).ToArray();
}
