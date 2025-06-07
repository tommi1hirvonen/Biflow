using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Biflow.Ui.TableEditor;

internal class ObservableDictionary<T, TU> : IDictionary<T, TU> where T : notnull
{
    private readonly Dictionary<T, TU> _dict;
    private readonly Action _onValueChanged;

    public ObservableDictionary(IDictionary<T, TU> collection, Action onValueChanged)
    {
        _dict = new(collection);
        _onValueChanged = onValueChanged;
    }

    public ObservableDictionary(Action onValueChanged)
    {
        _onValueChanged = onValueChanged;
        _dict = [];
    }

    TU IDictionary<T, TU>.this[T key]
    {
        get => _dict[key];
        set
        {
            _dict[key] = value;
            _onValueChanged();
        }
    }

    public ICollection<T> Keys => _dict.Keys;

    public ICollection<TU> Values => _dict.Values;

    public int Count => _dict.Count;

    public bool IsReadOnly => false;

    public void Add(T key, TU value)
    {
        _dict.Add(key, value);
    }

    public void Add(KeyValuePair<T, TU> item)
    {
        _dict.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _dict.Clear();
    }

    public bool Contains(KeyValuePair<T, TU> item)
    {
        return _dict.Contains(item);
    }

    public bool ContainsKey(T key)
    {
        return _dict.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<T, TU>[] array, int arrayIndex)
    {
        ((IDictionary<T, TU>)_dict).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<T, TU>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    public bool Remove(T key)
    {
        return _dict.Remove(key);
    }

    public bool Remove(KeyValuePair<T, TU> item)
    {
        return ((IDictionary<T, TU>)_dict).Remove(item);
    }

    public bool TryGetValue(T key, [MaybeNullWhen(false)] out TU value)
    {
        return _dict.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _dict.GetEnumerator();
    }
}
