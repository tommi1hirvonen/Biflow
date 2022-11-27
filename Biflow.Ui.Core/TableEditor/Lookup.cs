using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public class Lookup
{
    public DataTableLookup DataTableLookup { get; }

    public Type DisplayValueDatatype { get; }

    public IEnumerable<(object? Value, object? DisplayValue)> Values { get; }

    public Lookup(DataTableLookup dataTableLookup, Type displayValueDatatype, IEnumerable<(object? Value, object? DisplayValue)> values)
    {
        DataTableLookup = dataTableLookup;
        DisplayValueDatatype = displayValueDatatype;
        Values = values;
    }
}
