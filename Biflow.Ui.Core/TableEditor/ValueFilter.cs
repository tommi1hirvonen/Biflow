namespace Biflow.Ui.Core;

public class ValueFilter<T, U> : IFilter
    where T : struct
    where U : notnull, Enum
{
    public ValueFilter(U initialOperator)
    {
        TypedOperator1 = initialOperator;
        TypedOperator2 = initialOperator;
    }

    public bool Enabled1 { get; set; }

    public T TypedFilterValue1 { get; set; }

    public object FilterValue1 => TypedFilterValue1;

    public Enum Operator1 => TypedOperator1;

    public U TypedOperator1 { get; set; }

    public bool Enabled2 { get; set; }

    /// <summary>
    /// true => AND, false => OR
    /// </summary>
    public bool AndOr { get; set; } = true;

    public T TypedFilterValue2 { get; set; }

    public object FilterValue2 => TypedFilterValue2;

    public Enum Operator2 => TypedOperator2;

    public U TypedOperator2 { get; set; }
}
