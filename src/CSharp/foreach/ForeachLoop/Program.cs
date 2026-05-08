using ForeachLoop;
using ForeachLoop.ThroughEnumerable;

var arr = new int[] {1,2,3,4,5};
foreach(var item in arr)
{
    Console.WriteLine(item);
}

IEnumerable<int> enumerable = arr;
foreach (var item in enumerable)
{
    Console.WriteLine(item);
}

var list = new List<int> {1,2,3,4,5};
foreach (var item in list)
{
    Console.WriteLine(item);
}

var myCustomCollection = new MyCustomCollection();
foreach (var item in myCustomCollection)
{
    Console.WriteLine(item);
}


CustomCollection<string> customCollection = new CustomCollection<string>();
foreach (var item in customCollection)
{
    Console.WriteLine(item);
}

BadCollection badCollection = new BadCollection();
foreach (var x in badCollection)
{
    Console.WriteLine($"Outer: {x}");

    foreach (var y in badCollection)
    {
        Console.WriteLine($"  Inner: {y}");
    }
}