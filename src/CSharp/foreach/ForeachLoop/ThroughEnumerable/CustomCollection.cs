using System.Collections;

namespace ForeachLoop.ThroughEnumerable;

public class CustomCollection<T> : IEnumerable<T>
{
    public IEnumerator<T> GetEnumerator()
    {
        return new CustomEnumerator<T>();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator(); // IEnumerator -> IEnumerator<T> conversion is safe as IEnumerator<T> implements IEnumerator
    }
}