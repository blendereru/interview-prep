namespace Iterator;

public class Comparison
{
    public List<int> GetNumbers()
    {
        var numbers = new List<int>();

        for (var i = 0; i < 1000000; i++)
        {
            numbers.Add(i);
        }

        return numbers;
    }

    public IEnumerable<int> GetNumbersLazily()
    {
        for (var i = 0; i < 1000000; i++)
        {
            yield return i;
        }
    }
}