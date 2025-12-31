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