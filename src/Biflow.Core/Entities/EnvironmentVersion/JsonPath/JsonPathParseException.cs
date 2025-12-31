namespace Biflow.Core.Entities;

public sealed class JsonPathParseException : Exception
{
    public TextSpan Span { get; }
    public new string Source { get; }

    internal JsonPathParseException(string message, TextSpan span, string source) : base(message)
    {
        Span = span;
        Source = source;
    }

    public override string ToString()
    {
        var pointer = new string(' ', Span.Start) + "^";
        return $"""
                JSONPath parse error:
                {Message}
                {Source}
                {pointer}
                """;
    }
}
