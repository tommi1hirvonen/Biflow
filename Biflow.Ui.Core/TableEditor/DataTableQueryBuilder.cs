using Biflow.DataAccess.Models;
using Dapper;
using System.Text;

namespace Biflow.Ui.Core;

internal class DataTableQueryBuilder
{
    private readonly MasterDataTable _table;
    private readonly FilterSet? _filters;
    private readonly int? _top;

    public DataTableQueryBuilder(MasterDataTable table, int? top, FilterSet? filters)
    {
        _table = table;
        _filters = filters;
        _top = top;
    }

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
            ?? Array.Empty<(IFilter, Column, int)>();

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
                _ => throw new NotSupportedException($"Unsupoorted {nameof(LookupDisplayType)} value {dataTableLookup.LookupDisplayType}")
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

        if (activeFilters.Any())
        {
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
        }

        return (cmdBuilder.ToString(), parameters);
    }

    private static (string Statement, DynamicParameters Params) GenerateFilterStatement(string filterColumnIdentifier, Enum oper, object filterValue, int parameterIndex)
    {
        var statementBuilder = new StringBuilder();
        var parameters = new DynamicParameters();
        statementBuilder.Append($" {filterColumnIdentifier} ");
        if (oper is NumberFilterOperator nfo)
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
            if (nfo == NumberFilterOperator.IsBlank || nfo == NumberFilterOperator.IsNotBlank)
            {
                statementBuilder.Append(operatorText);
            }
            else
            {
                statementBuilder.Append(operatorText).Append("@Parameter_").Append(parameterIndex);
                parameters.Add($"Parameter_{parameterIndex}", filterValue);
            }
        }
        else if (oper is TextFilterOperator tfo)
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
            static string encodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
            var value = filterValue;
            if (tfo == TextFilterOperator.IsBlank || tfo == TextFilterOperator.IsNotBlank)
            {
                statementBuilder.Append(operatorText);
            }

            if (tfo == TextFilterOperator.Contains || tfo == TextFilterOperator.DoesNotContain)
            {
                value = $"%{encodeForLike(value.ToString() ?? "")}%";
            }
            else if (tfo == TextFilterOperator.StartsWith || tfo == TextFilterOperator.DoesNotStartWith)
            {
                value = $"{encodeForLike(value.ToString() ?? "")}%";
            }
            else if (tfo == TextFilterOperator.EndsWith || tfo == TextFilterOperator.DoesNotEndWith)
            {
                value = $"%{encodeForLike(value.ToString() ?? "")}";
            }

            if (tfo != TextFilterOperator.IsBlank && tfo != TextFilterOperator.IsNotBlank)
            {
                statementBuilder.Append(operatorText).Append("@Parameter_").Append(parameterIndex);
                parameters.Add($"Parameter_{parameterIndex}", value);
            }
        }
        else if (oper is BooleanFilterOperator bfo)
        {
            var operatorText = bfo switch
            {
                BooleanFilterOperator.Equals => " = ",
                _ => throw new ArgumentException($"Unsupported BooleanFilterOperator value {bfo}")
            };
            statementBuilder.Append(operatorText).Append("@Parameter_").Append(parameterIndex);
            parameters.Add($"Parameter_{parameterIndex}", filterValue);
        }
        else
        {
            throw new ArgumentException($"Unsupported filter operator type {oper.GetType()}");
        }
        return (statementBuilder.ToString(), parameters);
    }
}
