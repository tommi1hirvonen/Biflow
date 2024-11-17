namespace Biflow.Ui.TableEditor;

public class ValueFilter<T, TU>(TU initialOperator) : IFilter
    where T : struct
    where TU : Enum
{
    public bool Enabled1 { get; set; }

    public T TypedFilterValue1 { get; set; }

    public object FilterValue1 => TypedFilterValue1;

    public Enum Operator1 => TypedOperator1;

    public TU TypedOperator1 { get; set; } = initialOperator;

    public bool Enabled2 { get; set; }

    /// <summary>
    /// true => AND, false => OR
    /// </summary>
    public bool AndOr { get; set; } = true;

    public T TypedFilterValue2 { get; set; }

    public object FilterValue2 => TypedFilterValue2;

    public Enum Operator2 => TypedOperator2;

    public TU TypedOperator2 { get; set; } = initialOperator;
}
