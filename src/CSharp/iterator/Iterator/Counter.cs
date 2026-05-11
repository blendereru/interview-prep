namespace Iterator;

class Counter
{
    private int _start;

    public Counter(int start)
    {
        _start = start;
    }

    public IEnumerable<int> Count()
    {
        yield return _start;
        yield return _start + 1;
        yield return _start + 2;
    }
}