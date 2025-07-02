using ClosedXML.Excel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Biflow.Ui.TableEditor;

public class TableData
{
    private readonly LinkedList<Row> _rows;
    private readonly Column[] _columns;

    internal MasterDataTable MasterDataTable { get; }

    public IEnumerable<Column> Columns => _columns;

    public bool HasChanges { get; internal set; }

    public bool HasMoreRows { get; }

    internal TableData(
        MasterDataTable masterDataTable,
        Column[] columns,
        IDictionary<string, object?>[] data,
        bool hasMoreRows)
    {
        MasterDataTable = masterDataTable;
        _columns = columns;
        var dateColumns = columns.Where(c => c.Datatype == typeof(DateOnly)).ToArray();
        if (dateColumns.Length > 0)
        {
            foreach (var row in data)
            {
                foreach (var col in dateColumns)
                {
                    var value = row[col.Name];
                    if (value is DateTime dt)
                    {
                        row[col.Name] = DateOnly.FromDateTime(dt);
                    }
                }
            }
        }
        var timeColumns = columns.Where(c => c.Datatype == typeof(TimeOnly)).ToArray();
        if (timeColumns.Length > 0)
        {
            foreach (var row in data)
            {
                foreach (var col in timeColumns)
                {
                    var value = row[col.Name];
                    if (value is TimeSpan ts)
                    {
                        row[col.Name] = TimeOnly.FromTimeSpan(ts);
                    }
                }
            }
        }
        _rows = new LinkedList<Row>(data.Select(row => new Row(this, masterDataTable.AllowUpdate, row)));
        HasMoreRows = hasMoreRows;
    }

    public bool IsEditable => _columns.Any(c => c.IsPrimaryKey);

    public FilterSet EmptyFilterSet => new(_columns);

    public IEnumerable<Row> Rows => _rows.Where(r => !r.ToBeDeleted);

    public async Task<(int Inserted, int Updated, int Deleted)> SaveChangesAsync()
    {
        var changes = _rows
            .OrderByDescending(row => row.ToBeDeleted) // handle records to be deleted first
            .Select(row => row.GetChangeSqlCommand())
            .OfType<RowChangeSqlCommand>()
            .ToArray();

        if (changes.Length == 0)
        {
            return (0, 0, 0);
        }

        await using var connection = new SqlConnection(MasterDataTable.Connection.ConnectionString);
        return await MasterDataTable.Connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var (command, parameters, _) in changes)
                {
                    await connection.ExecuteAsync(command, parameters, transaction);
                }
                await transaction.CommitAsync();
                var inserted = changes.Count(c => c.CommandType == CommandType.Insert);
                var updated = changes.Count(c => c.CommandType == CommandType.Update);
                var deleted = changes.Count(c => c.CommandType == CommandType.Delete);
                return (inserted, updated, deleted);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public void AddRow(Row? other = null)
    {
        if (!MasterDataTable.AllowInsert)
        {
            throw new InvalidOperationException("Inserting records is not allowed on this data table.");
        }
        HasChanges = true;
        if (other is null)
        {
            _rows.AddFirst(new Row(this, true));
        }
        else
        {
            var copy = new Row(other);
            var node = _rows.Find(other);
            ArgumentNullException.ThrowIfNull(node);
            _rows.AddAfter(node, copy);
        }
        
    }

    public Stream GetExcelExportStream()
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");

        var exportColumns = _columns
            .Where(c => !c.IsHidden || c.IsPrimaryKey && MasterDataTable.AllowImport)
            .ToArray();

        var exportColumnNames = exportColumns.Select(c => c.Name).ToArray();

        // Add column headers.
        // Only include columns that are not hidden.
        // If 'allow import' is enabled, include hidden columns if they are part of the primary key.
        var allColumnNames = new List<string>();
        foreach (var col in exportColumns)
        {
            allColumnNames.Add(col.Name);
            sheet.Cell(1, allColumnNames.Count).SetValue(col.Name);

            if (!LookupColumnShouldBeAdded(col, out _))
            {
                continue;
            }
            
            // If the column has a lookup where the display type is 'Description' or 'ValueAndDescription',
            // also add the lookup value.
            const string suffix = "Description";
            var suffixInt = 2;
            var columnName = $"{col.Name} {suffix}";
            // Ensure generated column names are unique.
            while (allColumnNames.Contains(columnName) || exportColumnNames.Contains(columnName))
            {
                columnName = $"{col.Name} {suffix}{suffixInt}";
                suffixInt++;
            }
            allColumnNames.Add(columnName);
            sheet.Cell(1, allColumnNames.Count).SetValue(columnName);
        }

        // Add row data.
        var rowIndex = 2;
        foreach (var row in _rows)
        {
            var colIndex = 1;
            foreach (var column in exportColumns)
            {
                var value = row.Values[column.Name];
                // Excel does not handle DateOnly and TimeOnly types.
                // Convert them to DateTime.
                var cellValue = MakeExcelCompatibleValue(value);
                sheet.Cell(rowIndex, colIndex).Value = XLCellValue.FromObject(cellValue);
                colIndex++;
                
                // Check whether the lookup value should be added.
                if (!LookupColumnShouldBeAdded(column, out var lookup))
                {
                    continue;
                }
                
                var lookupValueOrNull = lookup.Values.FirstOrDefault(v => v.Value?.Equals(value) == true);
                var lookupValue = MakeExcelCompatibleValue(lookupValueOrNull?.DisplayValue ?? value);
                sheet.Cell(rowIndex, colIndex).Value = XLCellValue.FromObject(lookupValue);
                colIndex++;
            }
            rowIndex++;
        }
        var firstCell = sheet.Cell(1, 1);
        // Ensure columnCount > 0.
        var columnCount = allColumnNames.Count == 0 ? 1 : allColumnNames.Count;
        var lastCell = sheet.Cell(_rows.Count + 1, columnCount); // Add 1 to the row count to account for the header row
        var range = sheet.Range(firstCell, lastCell);
        range.CreateTable();

        // Adjust column widths only when running on Windows,
        // as the required fonts may be missing on non-Windows systems.
        // https://github.com/ClosedXML/ClosedXML/wiki/Graphic-Engine/8ee9bf5415f5e590da01c676baa71e118e76f31c
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Adjust column widths based on only the first 100 rows for much better performance.
            sheet.Columns(1, columnCount).AdjustToContents(1, 100);
        }
        
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static object? MakeExcelCompatibleValue(object? value) => value switch
    {
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        TimeOnly time => DateTime.MinValue.Add(time.ToTimeSpan()),
        not null => value,
        null => null
    };

    private static bool LookupColumnShouldBeAdded(Column column, [MaybeNullWhen(false)] out Lookup lookup)
    {
        if (column.Lookup is 
            {
                DataTableLookup.LookupDisplayType:
                LookupDisplayType.Description or LookupDisplayType.ValueAndDescription
            })
        {
            lookup = column.Lookup;
            return true;
        }

        lookup = null;
        return false;
    }
}
