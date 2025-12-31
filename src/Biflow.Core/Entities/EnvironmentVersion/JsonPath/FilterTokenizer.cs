namespace Biflow.Core.Entities;

internal static class FilterTokenizer
{
    public static List<FilterToken> Tokenize(string input)
    {
        var tokens = new List<FilterToken>();
        int i = 0;

        while (i < input.Length)
        {
            if (char.IsWhiteSpace(input[i]))
            {
                i++;
                continue;
            }

            if (input.Substring(i).StartsWith("&&"))
            {
                tokens.Add(new(FilterTokenType.And, "&&", new TextSpan(i, 2)));
                i += 2;
            }
            else if (input.Substring(i).StartsWith("||"))
            {
                tokens.Add(new(FilterTokenType.Or, "||", new TextSpan(i, 2)));
                i += 2;
            }
            else if (input.Substring(i).StartsWith("!="))
            {
                // Let comparison parser handle !=
                ReadComparison(input, ref i, tokens);
            }
            else if (input[i] == '!')
            {
                tokens.Add(new(FilterTokenType.Not, "!", new TextSpan(i, 1)));
                i++;
            }
            else if (input[i] == '(')
            {
                tokens.Add(new(FilterTokenType.LParen, "(", new TextSpan(i, 1)));
                i++;
            }
            else if (input[i] == ')')
            {
                tokens.Add(new(FilterTokenType.RParen, ")", new TextSpan(i, 1)));
                i++;
            }
            else
            {
                ReadComparison(input, ref i, tokens);
            }
        }

        return tokens;
    }

    private static void ReadComparison(string input, ref int i, List<FilterToken> tokens)
    {
        int start = i;
        int depth = 0;

        while (i < input.Length)
        {
            if (input[i] == '(') depth++;
            if (input[i] == ')') break;

            if (depth == 0 &&
                (input.Substring(i).StartsWith("&&") ||
                 input.Substring(i).StartsWith("||")))
                break;

            if (depth == 0 && input[i] == '!' && !input.Substring(i).StartsWith("!="))
                break;

            i++;
        }
        
        // ... scan forward
        int length = i - start;

        tokens.Add(new FilterToken(
            FilterTokenType.Comparison,
            input[start..i].Trim(),
            new TextSpan(start, length)));
    }
}

