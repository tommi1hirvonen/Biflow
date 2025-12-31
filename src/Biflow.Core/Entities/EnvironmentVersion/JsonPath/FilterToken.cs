namespace Biflow.Core.Entities;

internal enum FilterTokenType
{
    And,
    Or,
    Not,
    LParen,
    RParen,
    Comparison
}

internal sealed record FilterToken(FilterTokenType Type, string Text, TextSpan Span );