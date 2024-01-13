namespace Biflow.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class CategoryAttribute(string name, int ordinal) : Attribute
{
    public string Name { get; } = name;

    public int Ordinal { get; } = ordinal;
}
