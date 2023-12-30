using Dapper;

namespace Biflow.Ui.TableEditor;

internal record RowChangeSqlCommand(string SqlCommand, DynamicParameters Parameters, CommandType CommandType);