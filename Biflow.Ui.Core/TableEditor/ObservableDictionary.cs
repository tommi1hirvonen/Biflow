using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Biflow.Ui.Core;
internal class ObservableDictionary<T, U> : IDictionary<T, U> where T : notnull
{
    private readonly Dictionary<T, U> _dict;
    private readonly Action _onValueChanged;

    public ObservableDictionary(IDictionary<T, U> collection, Action onValueChanged)
    {
        _dict = new(collection);
        _onValueChanged = onValueChanged;
    }

    public ObservableDictionary(Action onValueChanged)
    {
        _onValueChanged = onValueChanged;
        _dict = new();
    }

    U IDictionary<T, U>.this[T key]
    {
        get => _dict[key];
        set
        {
            _onValueChanged();
            _dict[key] = value;
        }
    }

    public ICollection<T> Keys => _dict.Keys;

    public ICollection<U> Values => _dict.Values;

    public int Count => _dict.Count;

    public bool IsReadOnly => false;

    public void Add(T key, U value)
    {
        _dict.Add(key, value);
    }

    public void Add(KeyValuePair<T, U> item)
    {
        _dict.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _dict.Clear();
    }

    public bool Contains(KeyValuePair<T, U> item)
    {
        return _dict.Contains(item);
    }

    public bool ContainsKey(T key)
    {
        return _dict.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<T, U>[] array, int arrayIndex)
    {
        ((IDictionary<T, U>)_dict).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<T, U>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    public bool Remove(T key)
    {
        return _dict.Remove(key);
    }

    public bool Remove(KeyValuePair<T, U> item)
    {
        return ((IDictionary<T, U>)_dict).Remove(item);
    }

    public bool TryGetValue(T key, [MaybeNullWhen(false)] out U value)
    {
        return _dict.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _dict.GetEnumerator();
    }
}
