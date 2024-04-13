using Biflow.Core;
using ClosedXML.Excel;
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
        IEnumerable<IDictionary<string, object?>> data,
        bool hasMoreRows)
    {
        MasterDataTable = masterDataTable;
        _columns = columns;
        _rows = new LinkedList<Row>(data.Select(row => new Row(this, masterDataTable.AllowUpdate, row)));
        HasMoreRows = hasMoreRows;
    }

    public bool IsEditable => _columns.Any(c => c.IsPrimaryKey);

    public FilterSet EmptyFilterSet => new(_columns);

    public IEnumerable<Row> Rows => _rows.Where(r => !r.ToBeDeleted);

    public async Task<(int Inserted, int Updated, int Deleted)> SaveChangesAsync()
    {
        var changes = _rows?
            .OrderByDescending(row => row.ToBeDeleted) // handle records to be deleted first
            .Select(row => row.GetChangeSqlCommand())
            .OfType<RowChangeSqlCommand>();

        if (changes is null || !changes.Any())
        {
            return (0, 0, 0);
        }

        using var connection = new SqlConnection(MasterDataTable.Connection.ConnectionString);
        return await MasterDataTable.Connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var (command, parameters, type) in changes)
                {
                    await connection.ExecuteAsync(command, parameters, transaction);
                }
                await transaction.CommitAsync();
                var inserted = changes.Where(c => c.CommandType == CommandType.Insert).Count();
                var updated = changes.Where(c => c.CommandType == CommandType.Update).Count();
                var deleted = changes.Where(c => c.CommandType == CommandType.Delete).Count();
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

        var index = 1;
        // Only include columns that are not hidden.
        // If 'allow import' is enabled, include hidden columns if they are part of the primary key.
        foreach (var col in exportColumns)
        {
            sheet.Cell(1, index).SetValue(col.Name);
            index++;
        }
        var rowIndex = 2;
        foreach (var row in _rows)
        {
            var colIndex = 1;
            foreach (var column in exportColumns)
            {
                sheet.Cell(rowIndex, colIndex).Value = XLCellValue.FromObject(row.Values[column.Name]);
                colIndex++;
            }
            rowIndex++;
        }
        var firstCell = sheet.Cell(1, 1);
        var lastCell = sheet.Cell(_rows.Count + 1, exportColumns.Length); // Add 1 to row count to account for header row
        var range = sheet.Range(firstCell, lastCell);
        range.CreateTable();

        // Adjust column widths based on only the first 100 rows for much better performance.
        // Do this only when running on Windows, as the required fonts may be missing on non-Windows systems.
        // https://github.com/ClosedXML/ClosedXML/wiki/Graphic-Engine/8ee9bf5415f5e590da01c676baa71e118e76f31c
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            sheet.Columns(1, exportColumns.Length).AdjustToContents(1, 100);
        }
        
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
