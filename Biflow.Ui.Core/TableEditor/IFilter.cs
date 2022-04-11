namespace Biflow.Ui.Core;

public interface IFilter
{
    public bool Enabled1 { get; set; }

    public object? FilterValue1 { get; }

    public Enum Operator1 { get; }

    public bool Enabled2 { get; set; }

    /// <summary>
    /// true => AND, false => OR
    /// </summary>
    public bool AndOr { get; set; }

    public object? FilterValue2 { get; }

    public Enum Operator2 { get; }
}
