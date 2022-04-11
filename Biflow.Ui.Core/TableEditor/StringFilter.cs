namespace Biflow.Ui.Core;

public class StringFilter : IFilter
{
    public bool Enabled1 { get; set; }

    public string TypedFilterValue1 { get; set; } = string.Empty;

    public object FilterValue1 => TypedFilterValue1;

    public Enum Operator1 => TypedOperator1;

    public TextFilterOperator TypedOperator1 { get; set; }

    public bool Enabled2 { get; set; }

    /// <summary>
    /// true => AND, false => OR
    /// </summary>
    public bool AndOr { get; set; } = true;

    public string TypedFilterValue2 { get; set; } = string.Empty;

    public object FilterValue2 => TypedFilterValue2;

    public Enum Operator2 => TypedOperator2;

    public TextFilterOperator TypedOperator2 { get; set; }
}
