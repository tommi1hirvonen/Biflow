using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal static class JsonPathParser
{
    private static readonly char[] ValidPathChars = ['_', '-', '$'];
    
    public static List<IJsonPathSegment> Parse(string path, JsonNode root)
    {
        var segments = new List<IJsonPathSegment>();
        var i = 0;

        if (path[i] == '$')
        {
            segments.Add(new RootSegment(root));
            i++;
        }

        while (i < path.Length)
        {
            if (path[i] == '.')
            {
                if (i + 1 < path.Length && path[i + 1] == '.')
                {
                    segments.Add(new RecursiveDescentSegment());
                    i += 2;
                }
                else
                {
                    i++;
                    var start = i;
                    while (i < path.Length && (char.IsLetterOrDigit(path[i]) || ValidPathChars.Contains(path[i])))
                        i++;

                    segments.Add(new ChildSegment(path[start..i]));
                }
            }
            else if (path[i] == '*')
            {
                segments.Add(new WildcardSegment());
                i++;
            }
            else if (path[i] == '[')
            {
                if (path[i..].StartsWith("[*]"))
                {
                    segments.Add(new WildcardSegment());
                    i += 3;
                }
                else if (path[i..].StartsWith("[?("))
                {
                    var start = i + 3;
                    var end = path.IndexOf(")]", start, StringComparison.Ordinal);
                    if (end < 0)
                        throw new JsonPathParseException("Unclosed filter",
                            new TextSpan(start, path.Length - start),
                            path);

                    var expression = path[start..end];
                    var filter = FilterParser.Parse(expression);

                    segments.Add(new FilterSegment(filter));
                    i = end + 2;
                }
                else
                {
                    throw new NotSupportedException("Only wildcard and filter expressions are supported in brackets");
                }
            }
            else
            {
                throw new JsonPathParseException("Unexpected token", new TextSpan(i, 1), path);
            }
        }

        return segments;
    }
}
