namespace Biflow.Core.Entities;

internal sealed record FilterToken(FilterTokenType Type, string Text, TextSpan Span );