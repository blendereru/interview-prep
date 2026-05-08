using System.Collections;

namespace ForeachLoop.ThroughEnumerable;

public class CustomEnumerator<T> : IEnumerator<T>
{
    private readonly T[] _items;
    private int _index;
    public CustomEnumerator()
    {
        _items = new T[10];
        _index = -1;
    }
    
    public T Current => _items[_index];

    public bool MoveNext()
    {
        if (_index + 1 >= _items.Length)
        {
            return false;
        }
        _index++;
        return true;
    }

    public void Reset()
    {
        _index = -1;
    }

    object? IEnumerator.Current => Current;

    public void Dispose()
    {
        Console.WriteLine("Disposing");
    }
}