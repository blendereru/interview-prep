using Iterator;

foreach (var number in GetSingleDigitNumbersUpTo4())
{
    Console.WriteLine(number);
}

var comparison = new Comparison();
foreach (var number in comparison.GetNumbers().Take(5))
{
    Console.WriteLine(number);
}

foreach (var number in comparison.GetNumbersLazily().Take(5))
{
    Console.WriteLine(number);
}

foreach (var number in new Counter(3).Count())
{
    Console.WriteLine(number);
}

IEnumerable<int> GetOdd(int number)
{
    var temp = 1;
    while (temp < number)
    {
        if (temp % 2 != 0)
        {
            yield return temp;
        }
        temp++;
    }
}

IEnumerable<int> GetSingleDigitNumbers()
{
    yield return 0;
    yield return 1;
    yield return 2;
    yield return 3;
    yield return 4;
    yield return 5;
    yield return 6;
    yield return 7;
    yield return 8;
    yield return 9;
}

IEnumerable<int> GetSingleDigitNumbersUpTo4()
{
    yield return 0;
    yield return 1;
    yield return 2;
    yield return 3;
    yield return 4;
    yield break;
    yield return 5;
    yield return 6;
    yield return 7;
    yield return 8;
    yield return 9;
}