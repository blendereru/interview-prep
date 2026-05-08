using System.Collections;

namespace ForeachLoop;

public class BadCollection : IEnumerable<int>, IEnumerator<int>
{
    private int[] _data = { 1, 2, 3 };

    private int _position = -1;

    public IEnumerator<int> GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool MoveNext()
    {
        _position++;
        return _position < _data.Length;
    }

    public int Current => _data[_position];

    object IEnumerator.Current => Current;

    public void Reset()
    {
        _position = -1;
    }

    public void Dispose() { }
}