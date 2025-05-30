﻿using System.Text;

namespace Biflow.Ui.TableEditor;

internal class DataTableQueryBuilder(MasterDataTable table, int? top, FilterSet? filters)
{
    private readonly MasterDataTable _table = table;
    private readonly FilterSet? _filters = filters;
    private readonly int? _top = top;

    private string QuotedSchemaAndTable => $"{_table.TargetSchemaName.QuoteName()}.{_table.TargetTableName.QuoteName()}";

    public (string, DynamicParameters parameters) Build()
    {
        var cmdBuilder = new StringBuilder();
        var parameters = new DynamicParameters();
        var topStatement = _top is not null ? $"TOP {_top}" : "";
        cmdBuilder.Append($"""
            SELECT {topStatement} [main].*
            FROM {QuotedSchemaAndTable} AS [main]

            """);

        (IFilter, Column, int)[] activeFilters = _filters?.Filters
            .Where(f => f.Value.Enabled1)
            .Join(_filters.Columns,
                filter => filter.Key,
                column => column.Name,
                (filter, column) => (filter.Value, column))
            .Select((f, i) => (f.Value, f.column, i))
            .ToArray()
            ?? [];

        foreach (var (_, column, index) in activeFilters)
        {
            if (column.Lookup is null) continue;
            var dataTableLookup = column.Lookup.DataTableLookup;
            var columnName = dataTableLookup.ColumnName.QuoteName();
            var lookupSchema = dataTableLookup.LookupTable.TargetSchemaName.QuoteName();
            var lookupTable = dataTableLookup.LookupTable.TargetTableName.QuoteName();
            var lookupValueColumn = dataTableLookup.LookupValueColumn.QuoteName();
            var lookupFilterColumn = dataTableLookup.LookupDisplayType switch
            {
                LookupDisplayType.Value => dataTableLookup.LookupValueColumn.QuoteName(),
                LookupDisplayType.Description => dataTableLookup.LookupDescriptionColumn.QuoteName(),
                LookupDisplayType.ValueAndDescription => $"CONCAT({dataTableLookup.LookupValueColumn.QuoteName()}, ' ', {dataTableLookup.LookupDescriptionColumn.QuoteName()})",
                _ => throw new NotSupportedException($"Unsupported {nameof(LookupDisplayType)} value {dataTableLookup.LookupDisplayType}")
            };
            var lookupJoin = $"""
                LEFT JOIN (
                    SELECT {lookupValueColumn} AS [v], MAX({lookupFilterColumn}) AS [d]
                    FROM {lookupSchema}.{lookupTable}
                    GROUP BY {lookupValueColumn}
                ) AS [lookup{index}] ON [main].{columnName} = [lookup{index}].[v]

                """;
            cmdBuilder.Append(lookupJoin);
        }

        if (activeFilters.Length == 0)
        {
            return (cmdBuilder.ToString(), parameters);
        }
        
        cmdBuilder.Append(" WHERE ");
        var parameterIndex = 1;
        foreach (var (filter, column, index) in activeFilters)
        {
            if (parameterIndex > 1)
            {
                cmdBuilder.AppendLine().Append(" AND ");
            }
            cmdBuilder.Append('(');
            var filterColumnIdentifier = column.Lookup is not null
                ? $"[lookup{index}].[d]"
                : $"[main].{column.Name.QuoteName()}";
            var (statement1, paramsToAdd1) = GenerateFilterStatement(filterColumnIdentifier, filter.Operator1, filter.FilterValue1, parameterIndex);
            cmdBuilder.Append(statement1);
            parameters.AddDynamicParams(paramsToAdd1);
            if (filter.Enabled2)
            {
                parameterIndex++;
                var operand = filter.AndOr ? " AND " : " OR ";
                cmdBuilder.Append(operand);
                var (statement2, paramsToAdd2) = GenerateFilterStatement(filterColumnIdentifier, filter.Operator2, filter.FilterValue2, parameterIndex);
                cmdBuilder.Append(statement2);
                parameters.AddDynamicParams(paramsToAdd2);
            }
            cmdBuilder.Append(')');
            parameterIndex++;
        }

        return (cmdBuilder.ToString(), parameters);
    }

    private static (string Statement, DynamicParameters Params) GenerateFilterStatement(string filterColumnIdentifier, Enum @operator, object filterValue, int parameterIndex)
    {
        var statementBuilder = new StringBuilder();
        var parameters = new DynamicParameters();
        statementBuilder.Append($" {filterColumnIdentifier} ");
        switch (@operator)
        {
            case NumberFilterOperator nfo:
            {
                var operatorText = nfo switch
                {
                    NumberFilterOperator.Equals => " = ",
                    NumberFilterOperator.DoesNotEqual => " <> ",
                    NumberFilterOperator.GreaterThan => " > ",
                    NumberFilterOperator.GreaterThanOrEqual => " >= ",
                    NumberFilterOperator.LessThan => " < ",
                    NumberFilterOperator.LessThanOrEqual => " <= ",
                    NumberFilterOperator.IsBlank => " IS NULL",
                    NumberFilterOperator.IsNotBlank => " IS NOT NULL",
                    _ => throw new ArgumentException($"Unsupported NumberFilterOperator value {nfo}")
                };
                if (nfo is NumberFilterOperator.IsBlank or NumberFilterOperator.IsNotBlank)
                {
                    statementBuilder.Append(operatorText);
                }
                else
                {
                    statementBuilder.Append(operatorText).Append("@Parameter_").Append(parameterIndex);
                    parameters.Add($"Parameter_{parameterIndex}", filterValue);
                }
                break;
            }
            case TextFilterOperator tfo:
            {
                var operatorText = tfo switch
                {
                    TextFilterOperator.Equals => " = ",
                    TextFilterOperator.DoesNotEqual => " <> ",
                    TextFilterOperator.Contains => " LIKE ",
                    TextFilterOperator.DoesNotContain => " NOT LIKE ",
                    TextFilterOperator.StartsWith => " LIKE ",
                    TextFilterOperator.DoesNotStartWith => " NOT LIKE ",
                    TextFilterOperator.EndsWith => " LIKE ",
                    TextFilterOperator.DoesNotEndWith => " NOT LIKE ",
                    TextFilterOperator.GreaterThan => " > ",
                    TextFilterOperator.GreaterThanOrEqual => " >= ",
                    TextFilterOperator.LessThan => " < ",
                    TextFilterOperator.LessThanOrEqual => " <= ",
                    TextFilterOperator.IsBlank => " IS NULL",
                    TextFilterOperator.IsNotBlank => " IS NOT NULL",
                    _ => throw new ArgumentException($"Unsupported TextFilterOperator value {tfo}")
                };
                var value = filterValue;
                switch (tfo)
                {
                    case TextFilterOperator.IsBlank or TextFilterOperator.IsNotBlank:
                        statementBuilder.Append(operatorText);
                        break;
                    case TextFilterOperator.Contains or TextFilterOperator.DoesNotContain:
                        value = $"%{EncodeForLike(value.ToString() ?? "")}%";
                        break;
                    case TextFilterOperator.StartsWith or TextFilterOperator.DoesNotStartWith:
                        value = $"{EncodeForLike(value.ToString() ?? "")}%";
                        break;
                    case TextFilterOperator.EndsWith or TextFilterOperator.DoesNotEndWith:
                        value = $"%{EncodeForLike(value.ToString() ?? "")}";
                        break;
                }
                if (tfo is not TextFilterOperator.IsBlank and not TextFilterOperator.IsNotBlank)
                {
                    statementBuilder.Append(operatorText).Append("@Parameter_").Append(parameterIndex);
                    parameters.Add($"Parameter_{parameterIndex}", value);
                }
                break;
                
                static string EncodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
            }
            case BooleanFilterOperator bfo:
            {
                var operatorText = bfo switch
                {
                    BooleanFilterOperator.Equals => " = ",
                    _ => throw new ArgumentException($"Unsupported BooleanFilterOperator value {bfo}")
                };
                statementBuilder.Append(operatorText).Append("@Parameter_").Append(parameterIndex);
                parameters.Add($"Parameter_{parameterIndex}", filterValue);
                break;
            }
            default:
                throw new ArgumentException($"Unsupported filter operator type {@operator.GetType()}");
        }
        return (statementBuilder.ToString(), parameters);
    }
}
