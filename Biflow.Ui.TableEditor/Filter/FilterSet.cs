namespace Biflow.Ui.TableEditor;

public class FilterSet
{
    public Dictionary<string, IFilter> Filters { get; } = [];

    public FilterIndexer<ValueFilter<byte, NumberFilterOperator>> ByteIndexer { get; }
    public FilterIndexer<ValueFilter<short, NumberFilterOperator>> ShortIndexer { get; }
    public FilterIndexer<ValueFilter<int, NumberFilterOperator>> IntIndexer { get; }
    public FilterIndexer<ValueFilter<long, NumberFilterOperator>> LongIndexer { get; }
    public FilterIndexer<ValueFilter<decimal, NumberFilterOperator>> DecimalIndexer { get; }
    public FilterIndexer<ValueFilter<double, NumberFilterOperator>> DoubleIndexer { get; }
    public FilterIndexer<ValueFilter<float, NumberFilterOperator>> FloatIndexer { get; }
    public FilterIndexer<StringFilter> StringIndexer { get; }
    public FilterIndexer<ValueFilter<bool, BooleanFilterOperator>> BooleanIndexer { get; }
    public FilterIndexer<ValueFilter<DateTime, NumberFilterOperator>> DateTimeIndexer { get; }

    public IEnumerable<Column> Columns { get; }

    public FilterSet(IEnumerable<Column> columns)
    {
        ByteIndexer = new(Filters);
        ShortIndexer = new(Filters);
        IntIndexer = new(Filters);
        LongIndexer = new(Filters);
        DecimalIndexer = new(Filters);
        DoubleIndexer = new(Filters);
        FloatIndexer = new(Filters);
        StringIndexer = new(Filters);
        BooleanIndexer = new(Filters);
        DateTimeIndexer = new(Filters);

        Columns = columns;
        foreach (var columnInfo in Columns)
        {
            var column = columnInfo.Name;
            var datatype = columnInfo.Lookup?.DisplayValueDatatype ?? columnInfo.Datatype;
            if (datatype == typeof(byte))
                ByteIndexer[column] = new ValueFilter<byte, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(short))
                ShortIndexer[column] = new ValueFilter<short, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(int))
                IntIndexer[column] = new ValueFilter<int, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(long))
                LongIndexer[column] = new ValueFilter<long, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(decimal))
                DecimalIndexer[column] = new ValueFilter<decimal, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(double))
                DoubleIndexer[column] = new ValueFilter<double, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(float))
                FloatIndexer[column] = new ValueFilter<float, NumberFilterOperator>(NumberFilterOperator.Equals);
            else if (datatype == typeof(string))
                StringIndexer[column] = new StringFilter();
            else if (datatype == typeof(bool))
                BooleanIndexer[column] = new ValueFilter<bool, BooleanFilterOperator>(BooleanFilterOperator.Equals);
            else if (datatype == typeof(DateTime))
                DateTimeIndexer[column] = new ValueFilter<DateTime, NumberFilterOperator>(NumberFilterOperator.Equals);
            else
                throw new ArgumentException($"Unsupported datatype [{columnInfo.DbDatatype}]");
        }
    }
}
