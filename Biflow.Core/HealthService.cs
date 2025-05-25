using System.Collections.Concurrent;

namespace Biflow.Core;

public class HealthService
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _errors = [];
    
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors =>
        _errors.ToDictionary(x => x.Key, IReadOnlyList<string> (x) => x.Value.ToArray());
    
    public void AddError(Guid key, string errorMessage) => AddError(key.ToString(), errorMessage);

    public void AddError(string key, string errorMessage)
    {
        if (_errors.TryGetValue(key, out var errors) && !errors.Contains(errorMessage))
        {
            errors.Add(errorMessage);
            return;
        }

        _errors.TryAdd(key, [errorMessage]);
    }
    
    public void ClearErrors()
    {
        _errors.Clear();
    }
}