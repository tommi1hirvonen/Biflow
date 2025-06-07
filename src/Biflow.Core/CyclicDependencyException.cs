namespace Biflow.Core;

public class CyclicDependencyException<T>(List<List<T>> cyclicObjects, string message)
    : CyclicDependencyException(
        cyclicObjects.Select(x => x.Cast<object>().ToArray()).ToArray(), message)
{
    public new IEnumerable<IEnumerable<T>> CyclicObjects { get; } = cyclicObjects;
}

public class CyclicDependencyException(object[][] cyclicObjects, string message)
    : Exception(message)
{
    public IEnumerable<IEnumerable<object>> CyclicObjects { get; } = cyclicObjects;
}