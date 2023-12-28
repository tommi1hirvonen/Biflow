using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Biflow.Ui.TableEditor;

/// <summary>
/// Utility class that provides IDataReader functionality for IEnumerables of IDictionaries.
/// Only implements IDataReader members and methods which are required for
/// System.Data.SqlBulkCopy.WriteToServerAsync(IDataReader) to function.
/// </summary>
internal class DictionaryReader(IEnumerable<string> columns, ICollection<IDictionary<string, object?>> data) : IDataReader
{
    private readonly IEnumerator<IDictionary<string, object?>> _enumerator = data.GetEnumerator();
    private readonly IDictionary<string, int> _nameToIndexMap = columns.Select((c, i) => (c, i)).ToDictionary(key => key.c, value => value.i);
    private bool _isOpen;

    public object this[string name] => _enumerator.Current[name] ?? DBNull.Value;

    public object this[int i] => _enumerator.Current.ElementAt(i).Value ?? DBNull.Value;

    public int Depth => 1;

    public bool IsClosed => !_isOpen;

    public int RecordsAffected => -1;

    public int FieldCount => _nameToIndexMap.Count;

    public void Close()
    {
        _isOpen= false;
        Dispose();
    }

    public void Dispose() => _enumerator.Dispose();

    public bool Read()
    {
        _isOpen = true;
        return _enumerator.MoveNext();
    }

    public DataTable? GetSchemaTable() => null;

    public int GetOrdinal(string name) => _nameToIndexMap[name];

    public object GetValue(int i) => this[i];

    #region Not implemented IDataReader members and methods

    public bool GetBoolean(int i)
    {
        throw new NotImplementedException();
    }

    public byte GetByte(int i)
    {
        throw new NotImplementedException();
    }

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public char GetChar(int i)
    {
        throw new NotImplementedException();
    }

    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public IDataReader GetData(int i)
    {
        throw new NotImplementedException();
    }

    public string GetDataTypeName(int i)
    {
        throw new NotImplementedException();
    }

    public DateTime GetDateTime(int i)
    {
        throw new NotImplementedException();
    }

    public decimal GetDecimal(int i)
    {
        throw new NotImplementedException();
    }

    public double GetDouble(int i)
    {
        throw new NotImplementedException();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type GetFieldType(int i)
    {
        throw new NotImplementedException();
    }

    public float GetFloat(int i)
    {
        throw new NotImplementedException();
    }

    public Guid GetGuid(int i)
    {
        throw new NotImplementedException();
    }

    public short GetInt16(int i)
    {
        throw new NotImplementedException();
    }

    public int GetInt32(int i)
    {
        throw new NotImplementedException();
    }

    public long GetInt64(int i)
    {
        throw new NotImplementedException();
    }

    public string GetName(int i)
    {
        throw new NotImplementedException();
    }

    public string GetString(int i)
    {
        throw new NotImplementedException();
    }

    public int GetValues(object[] values)
    {
        throw new NotImplementedException();
    }

    public bool IsDBNull(int i)
    {
        throw new NotImplementedException();
    }

    public bool NextResult()
    {
        throw new NotImplementedException();
    }

    #endregion
}
