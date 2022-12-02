using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public class Lookup
{
    public MasterDataTableLookup DataTableLookup { get; }

    public Type DisplayValueDatatype { get; }

    public IEnumerable<(object? Value, object? DisplayValue)> Values { get; }

    public Lookup(MasterDataTableLookup dataTableLookup, Type displayValueDatatype, IEnumerable<(object? Value, object? DisplayValue)> values)
    {
        DataTableLookup = dataTableLookup;
        DisplayValueDatatype = displayValueDatatype;
        Values = values;
    }
}
