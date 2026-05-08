## Foreach loop
Besides `for`, `while` and `do-while` loops, c# also has a loop called `foreach`. The enumeration pattern in such loop
is intuitive and easy to understand:
```csharp
foreach (var item in collection) 
{
    // do something
}
```
These kind of loops are very useful when collection through which you are iterating might not contain fixed length.
Consider, the example of iterating through an array:
```csharp
var arr = new int[] {1,2,3,4,5};
foreach(var item in arr)
{
    Console.WriteLine(item);
}
```
So far, so good. The `foreach` loop statement can be used with an instance of any type that satisifes the [following](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-foreach-statement) conditions:
* A type has the public parameterless `GetEnumerator` method. The `GetEnumerator` method can be an extension method.
* The return type of the `GetEnumerator` method has the public `Current` property and the public parameterless `MoveNext` method whose return type is `bool`.

The runtime doesn't directly support the `foreach` statement, instead it gets transformed into the code [below](https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQAwAIpwCwG5lQDMmM6AwugN7Lq2bFTboCyAFAJRU108BuAhgCd0AGwCWAZwAu6ALzoAdgFMA7ugAykqQB4xCqQD4qcADQwThE9hMBWAL74kAMwD2gpfwDGAC3SsBwmJSSgC26HqiWuzI1Eg8WACcrEGh7I523LQZSHZAA=):
```csharp
var list = new List<int> {1,2,3,4,5};
foreach (var item in list)
{
    Console.WriteLine(item);
}

// this gets transformed into the following:

List<int> list2 = list;
List<int>.Enumerator enumerator = list2.GetEnumerator();
try
{
    while (enumerator.MoveNext())
    {
        int current = enumerator.Current;
        Console.WriteLine(current);
    }
}
finally
{
    ((IDisposable)enumerator).Dispose();
}
```
As you can see, `MoveNext` method is needed to loop until it becomes false, and `Current` property is used to get the current value.
Thus, we can create our [own enumerable collection](src/CSharp/foreach/ForeachLoop/MyCustomCollection.cs). Its transformation
is provided [here](https://sharplab.io/#v2:CYLg1APgAgDABFAjAOgDIEsB2BHA3AWACgoAmRIqAZgRLgGE4BvIuVhaqAFjgFkAKAJRMWbUQDcAhgCc4AWwCedAK4BnAC4B7WXQ0AbXQFMAxmvQbMcALxxMBgO69Fqzdr2GTZzIIKEAZhqkDCSMACzg+SRl0NQNZOCw5J3UtHX1jU3MBImZCUSQATj5o2IEfAF8RVgrCaooOWh4kl1T3DMxsyvZHZWTZAFFMJVkDKQlNGQBxAzUBoZGxgMErAD4be27nLVnh0fHvIlrieo3e7fnxjty2AAcpdEkYuECJYHNdeROXPsNhzDUAbQAunAAPoKHpfH4GP4qHyiW73MYGeJ/UFYYAGAAePk6VE+W0GOwWUkEnRyolEYKaWyhMKsawcjQhNNi0IBiBggLhFNYnVEZ1GACNDMgAEoSTAAcwMfBgABo4ByBMgACoaDDqQTIABiAT6wRCRRWoPBm36tLUKn+6GB1lsjOp5tZqMYADUJLolMjrOgyqU+WwQeisfSALSIblVTq46hMs3fZ1qehKKSBVGWVZU5lO36W/5BzAYzFcogxuCCjR6XgaMQGAByWLUpKurHJPPivnCBaLcDAirgy2sWfjFpUaGhkrUISyLYpbfbbCgAHY4L4PSoDJGKdUF92sWAwFu8iu1FIvVvqoc8aR8Tm2Zc8tQsEn3Z7kYw4NK1Lg4Bvv3BLyAA=)

## IEnumerator and IEnumerable
`IEnumerator` and `IEnumerator<T>` are interfaces by implementing which you implement the `MoveNext` and `Current` methods.
In addition to that, `IEnumerator` contains a method called `Reset`, which is not used during enumeration at all. But as its
name states, it is needed to reset the enumerator to the beginning of the collection. `IEnumerator<T>` is a generic(type-safe) implements `IEnumerator`
and adds a `Current` property of type `T`.

`IEnumerable` and `IEnumerable<T>` are interfaces by implementing which you implement the `GetEnumerator` method.
As `IEnumerable<T>` implements `IEnumerable`, implementing `IEnumerable<T>` requires implementing both `GetEnumerator` and `GetEnumerator<T>`.

## Problem
In the [bad example](src/CSharp/foreach/ForeachLoop/BadCollection.cs), it is shown that every call to `GetEnumerator` method points to the same instance of `BadCollection`.
This is not a good practice. Because `foreach` loops might be nested. Consider the example:
```csharp
BadCollection badCollection = new BadCollection();
foreach (var x in badCollection)
{
    Console.WriteLine($"Outer: {x}");

    foreach (var y in badCollection)
    {
        Console.WriteLine($"  Inner: {y}");
    }
}
```
After the first iteration(outer loop), `badCollection` enters inner loop, where inner loop also iterates over the same collection.
But because `GetEnumerator` method returns the same instance of `BadCollection`, the inner loop iterates starting from
the first index, then iterates again and exits the loop(no elements left). Thus, outer loop is not called again. That is why as stated
in the following [article](https://learn.microsoft.com/en-us/archive/msdn-magazine/2017/april/essential-net-understanding-csharp-foreach-internals-and-custom-iterators-with-yield#what-makes-a-class-a-collection),
think of enumerator as a “cursor” or a “bookmark” in the sequence. You can have multiple bookmarks, and moving any one of them enumerates over the collection independently of the others.
That is why returning new instance of enumerator is a safe practice. This solution is also a good practice when concurrent
access to the collection is happening.

## Some interesting stuff
1) `foreach` loops over arrays do not behave the same way as `foreach` loops over other collections(calling `MoveNext`, retrieving `Current`).
Even though, Arrays [implement](https://github.com/dotnet/dotnet/blob/b0f34d51fccc69fd334253924abd8d6853fad7aa/src/runtime/src/libraries/System.Private.CoreLib/src/System/Array.cs#L3062) `IEnumerable`,
compiler applies a special optimization, to transform into a [direct indexed loop](https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQMwAJboMLoN7LpGYZQAs6AsgBQCU+hxTAbgIYBO6HnAvOgHYBTAO7oAlvwAuAbQC6+OABoYi1IrKKArAF8A3MgBmAe3aDWAYwAW1NpzGTBAW3H8u7drWQEkTKHACc1PZOtPpI2oxEEeFAA).
It is done, because the overhead of calling `MoveNext` and `Current` is not worth, when we know that arrays are indexable in O(1).
Arrays support `IEnumerator`, as implicit conversion to `IEnumerable`(or it's generic analog) could be done:
```csharp
var arr = new int[] {1,2,3,4,5};

IEnumerable<int> enumerable = arr;
foreach (var item in enumerable)
{
    Console.WriteLine(item);
}

// translates into:

int[] array = new int[5];
int[] array2 = array;

IEnumerable<int> enumerable = array2;
IEnumerator<int> enumerator = enumerable.GetEnumerator();
try
{
    while (enumerator.MoveNext())
    {
        int current = enumerator.Current;
        Console.WriteLine(current);
    }
}
finally
{
    if (enumerator != null)
    {
        enumerator.Dispose();
    }
}
```
(Source code is [here](https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQAwAIpwCwG5lQDMmM6AwugN7Lq2bFTboCyAFAJRU108BuAhgCd0Q4QF50AOwCmAd3QBLSQBcA2gF0qcADQxthbdm0BWAL74kAMwD2g6fwDGAC1YDhC5dIC2iySMGC7MjUSDxYAJysHt7sFqbIBHCEADxKygB86NKSAK5e0oL8AEYANtLoEqIWNnaOTuiuQoqePkpZufmFpdJBSCFhcJHRXrHI8UjctOOmQA===))

2) As may be visible from code translations, `foreach` does not need `IEnumerable` to be implemented on the collection.
People say that `IEnumerable` and `IEnumerator` are needed for enumeration, but these are the interfaces that are needed to provide the contract.
As seen from `MyCustomCollection`, compiler only searches for `GetEnumerator` method, and `MoveNext` and `Current` methods on that enumerator.
This is called [duck typing](https://en.wikipedia.org/wiki/Duck_typing). And if duck typing fails to find a suitable implementation of the enumerable pattern,
the compiler checks whether the collection implements the interfaces.

## Links
The material for notes were taken from [this](https://learn.microsoft.com/en-us/archive/msdn-magazine/2017/april/essential-net-understanding-csharp-foreach-internals-and-custom-iterators-with-yield#essential-net) article