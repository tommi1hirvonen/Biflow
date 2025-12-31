namespace Biflow.Core.Entities;

internal static class FilterParser
{
    public static IJsonPathFilter Parse(string expression)
    {
        var tokens = FilterTokenizer.Tokenize(expression);
        var index = 0;

        var result = ParseOr(tokens, ref index, expression);

        if (index < tokens.Count)
        {
            var token = tokens[index];
            throw new JsonPathParseException("Unexpected trailing token", token.Span, expression);
        }

        return result;
    }

    private static IJsonPathFilter ParseOr(List<FilterToken> tokens, ref int i, string source)
    {
        var left = ParseAnd(tokens, ref i, source);

        while (i < tokens.Count && tokens[i].Type == FilterTokenType.Or)
        {
            i++;
            var right = ParseAnd(tokens, ref i, source);
            left = new OrFilter(left, right);
        }

        return left;
    }

    private static IJsonPathFilter ParseAnd(List<FilterToken> tokens, ref int i, string source)
    {
        var left = ParseUnary(tokens, ref i, source);

        while (i < tokens.Count && tokens[i].Type == FilterTokenType.And)
        {
            i++;
            var right = ParseUnary(tokens, ref i, source);
            left = new AndFilter(left, right);
        }

        return left;
    }

    private static IJsonPathFilter ParseUnary(List<FilterToken> tokens, ref int i, string source)
    {
        if (tokens[i].Type == FilterTokenType.Not)
        {
            i++;
            return new NotFilter(ParseUnary(tokens, ref i, source));
        }

        return ParsePrimary(tokens, ref i, source);
    }

    private static IJsonPathFilter ParsePrimary(List<FilterToken> tokens, ref int i, string source)
    {
        var token = tokens[i];

        if (token.Type == FilterTokenType.LParen)
        {
            i++;
            var expr = ParseOr(tokens, ref i, source);

            if (tokens[i].Type != FilterTokenType.RParen)
                throw new JsonPathParseException("Missing closing parenthesis", tokens[i].Span, source);

            i++;
            return expr;
        }

        if (token.Type == FilterTokenType.Comparison)
        {
            i++;
            return SimpleComparisonParser.Parse(token.Text);
        }

        throw new JsonPathParseException("Unexpected token", token.Span, source);
    }
}
