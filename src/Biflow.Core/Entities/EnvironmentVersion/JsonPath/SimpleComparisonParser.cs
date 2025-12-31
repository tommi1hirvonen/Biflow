using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal static class SimpleComparisonParser
{
    public static IJsonPathFilter Parse(string expression)
    {
        expression = expression.Trim();

        if (!expression.StartsWith("@."))
            throw new JsonPathParseException("Filter must start with @.",
                new TextSpan(0, expression.Length),
                expression);

        int opIndex = FindOperator(expression, out var op);

        var left = expression.Substring(2, opIndex - 2).Trim();
        var right = expression[(opIndex + OperatorLength(op))..].Trim();

        var path = left.Split('.', StringSplitOptions.RemoveEmptyEntries);
        object literal = ParseLiteral(right);

        return new PropertyComparisonFilter(
            path,
            node => Compare(node, op, literal)
        );
    }

    private static bool Compare(JsonNode? node, FilterOperator op, object literal)
    {
        if (node is not JsonValue value)
            return false;

        if (literal is decimal d && value.TryGetValue(out decimal n))
        {
            return op switch
            {
                FilterOperator.Eq => n == d,
                FilterOperator.Ne => n != d,
                FilterOperator.Lt => n < d,
                FilterOperator.Lte => n <= d,
                FilterOperator.Gt => n > d,
                FilterOperator.Gte => n >= d,
                _ => false
            };
        }

        if (literal is string s && value.TryGetValue(out string? v))
        {
            return op switch
            {
                FilterOperator.Eq => v == s,
                FilterOperator.Ne => v != s,
                _ => false
            };
        }

        if (literal is bool b && value.TryGetValue(out bool vb))
        {
            return op switch
            {
                FilterOperator.Eq => vb == b,
                FilterOperator.Ne => vb != b,
                _ => false
            };
        }

        return false;
    }

    private static int FindOperator(string expr, out FilterOperator op)
    {
        var operators = new Dictionary<string, FilterOperator>
        {
            ["=="] = FilterOperator.Eq,
            ["!="] = FilterOperator.Ne,
            ["<="] = FilterOperator.Lte,
            [">="] = FilterOperator.Gte,
            ["<"]  = FilterOperator.Lt,
            [">"]  = FilterOperator.Gt
        };

        foreach (var kv in operators)
        {
            int idx = expr.IndexOf(kv.Key, StringComparison.Ordinal);
            if (idx > 0)
            {
                op = kv.Value;
                return idx;
            }
        }

        throw new JsonPathParseException("Expected comparison operator (==, !=, <, <=, >, >=)",
            new TextSpan(0, expr.Length),
            expr);

    }

    private static int OperatorLength(FilterOperator op) =>
        op switch
        {
            FilterOperator.Eq or FilterOperator.Ne => 2,
            _ => 1
        };

    private static object ParseLiteral(string text)
    {
        if (text.StartsWith("'") && text.EndsWith("'"))
            return text[1..^1];

        if (bool.TryParse(text, out var b))
            return b;

        if (decimal.TryParse(text, out var n))
            return n;

        throw new JsonPathParseException("Expected literal value", new TextSpan(0, text.Length), text);
    }
}