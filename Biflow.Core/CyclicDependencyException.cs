namespace Biflow.Core;

public class CyclicDependencyException<T>(IEnumerable<IEnumerable<T>> cyclicObjects, string message)
    : CyclicDependencyException(cyclicObjects.Select(o => o.Cast<object>()), message)
{
    public new IEnumerable<IEnumerable<T>> CyclicObjects { get; } = cyclicObjects;
}

public class CyclicDependencyException(IEnumerable<IEnumerable<object>> cyclicObjects, string message)
    : Exception(message)
{
    public virtual IEnumerable<IEnumerable<object>> CyclicObjects { get; } = cyclicObjects;
}