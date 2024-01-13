namespace Biflow.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class DescriptionAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}