namespace ForeachLoop;

public class MyCustomCollection
{
    public MyCustomEnumerator GetEnumerator() => new MyCustomEnumerator();
}

public class MyCustomEnumerator
{
    private readonly MyCustomElement[] _myCustomElements;
    private int _index;

    public MyCustomEnumerator()
    {
        _myCustomElements = new MyCustomElement[10];
        
        Enumerable.Range(0, 10).ToList().ForEach(i => _myCustomElements[i] = new MyCustomElement {Value = i});
        _index = -1;
    }
    
    public MyCustomElement Current => _myCustomElements[_index];

    public bool MoveNext()
    {
        if (_index + 1 >= _myCustomElements.Length)
        {
            return false;
        }
        _index++;
        return true;
    }
}

public class MyCustomElement
{
    public int Value { get; set; }
}