## Delegates
Delegates in c# are basically references to methods. They are declared using the `delegate` keyword. The signature of the delegate
and the method it references should be the same. For example:
```csharp
public delegate int Calculate(int a, int b)
```
The delegate above can references can reference any method(whether class or struct or static) that takes two integer parameters
and returns an integer. Delegates allow methods to be passed around as variables. This is their typical use case.
We actually use delegates a lot, for example, in `LINQ` queries. Consider the following example:
```csharp
List<int> list = new List<int> { 1, 2, 3, 4, 5 };
var result = list.Where(x => x > 2);
```
The `Where` method takes a delegate(`Func`, which-is built-in) as a parameter, which allows to perform the filtering operation of any kind.
The `Where` clause uses anonymous method and lambda expression to define the filtering condition. So simplified version looks like this:
```csharp
public static class ListExtensions
{
    public delegate bool Predicate<TSource>(TSource item);

    public static IEnumerable<TSource> Where<TSource>(
        this List<TSource> list,
        Predicate<TSource> predicate)
    {
        foreach (var item in list)
        {
            if (predicate(item))
            {
                yield return item;
            }
        }
    }
    
    // usage
    List<int> list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    var filtered = list.Where(IsEven).ToList();

    bool IsEven(int x)
    {
        return x % 2 == 0;
    }
}
```
This actually shows the power of delegates. Delegates themselves don't know about the method they reference, so in the future, when the method
signature changes, nothing needs to be changed in the delegate definition. From this perspective, they are similar to interfaces. 
Generally, we don't need to explicitly declare delegates, because c# provides a lot of them out of the box. They are:
* `Action` - delegate that takes no parameters(or one or more parameters) and returns nothing:
```csharp
public delegate void Action<in T>(T obj)
        where T : allows ref struct;
```
* `Func` - delegate that takes no or more parameters and returns a value:
```csharp
public delegate TResult Func<in T, out TResult>(T arg) // might take no parameter at all
        where T : allows ref struct
        where TResult : allows ref struct;
```
* `Predicate` - delegate that takes a single parameter and returns a boolean value:
```csharp
public delegate bool Predicate<in T>(T obj) where T : allows ref struct;
```

Delegates are derived from `System.Delegate`(or `MulticastDelegate`, actually) class, which contains a number of methods:
* `Combine` - used to combine multiple delegates into one. Delegates support method chaining through `+` operator, so during
delegate invocation, the delegates are invoked in the order they were added. Consider [this](https://sharplab.io/#v2:CYLg1APgAgTAjAWAFDOQOwIYFsCmBnABwwGMcACAERwBscBzDAFxwFEAPbA2vAblSQD0AyjgBmASzTkMZYgFc8jAPZYywGvSblGACyZk9eMjKhwADGSIAnbDmZXjaYGSgB2I1AAsAOmTraDMwunmQAwgrKWKEY1NQARiQA1gAUphZ4AJR8KEiwZAAq+Iyh1Bh4eMgA3shktWRCIhJSZIwA7kpkuLpKwEa6+noAbto65Hi2ZHjidJiMclbSRuGKKtGxCcSJvkh1LnAAbMFkABI01Eqp5pMZNXXVO7t1pgCcyQAkAES1p7FKADRkSp4AC+AEIPllbrVgfxdqZDl4yABxJQ9OIATxwl3SNwetXujyecFen1qKLRmIBQLBEOyuxhOThByOAFkMJJkrjdgTCfVhFRiKUFmRJIoMGhSEYlKIWqNZBEVGoNIEcNteWEFVEYvEkgZxFRqACMTgDQCsHJqIx9TQzRarSzJApjtbqNkoY8GgBJNCy8RGHAcLBcHAA9FKOSycVkFTiRiy8jyFaqfyaILiGVhuTu3YNVriuPKRzOBTkUwwAA8aQAfCK0IocBhgGreUIy5XzDWdC6jZjTZ07d3+5bxA60E6XW68R7hN7Y+IYuIAF4jcgplXRuIAKxwxDjXYNsv0C1EOAWEvw8ezdQaXR0PROZyUzcJ+5oZAAvA/fpP1V60HOF2XeMlQCLQN23XcyGNA9+jjY9Txwc8+lGK9ahvOw72cclgGNZ9Hmgt9P2w40fxbYR8jlNoOjXLQ8ABV9qCLKDextYxhWIFQ4kkHBnGUVC+TIUQlCsVRzWHA08N2MSrQPT8GLIMBmJNGhSMJBoACUcCwJRhj1A9RCsRVdHIaTxGIMo4xo5gAVoDBBkkOglNNfjcy7YgdEjWIpTQah0WA2972IzFJLqUzR3HWSyGSUyDwAWj0mgMlBVTHheZIPm9QYlESByQNTcgGJAWl+IY9KAEFiqnJlXgytAspytBHKs8gCOoIrISqupWvSgAhSr1TS2r6ty5qhxkmh2rpXkYpodLQn63lBsy7KRuVMCwsdPBnQNSb+I2sctpddKKAW6FkAZAQACoyAAeTkRgCHukBkGWhqmrWoJCtuH5zgBMrQReuqVsavL11a56HiCkMyB6gGkFe1bQKCGa2u+x8AVCOGyVRHDKTCOGEZB0b9oiibbihgEKDhy6BCAA===)
example, where each `+` operator call is a Combine method call and every `-` operator call is a Remove method call.
* `Remove` - used to remove a delegate from a list of delegates. When you remove a delegate from a multicast delegate,
it removes the rightmost matching entry, so only one instance is removed if there are multiple copies.

## How Combine and Remove methods are actually implemented ?

### Combine
Combine method internally calls `CombineImpl` method:
```csharp
public static Delegate? Combine(Delegate? a, Delegate? b)
{
    if (a is null)
        return b;

    return a.CombineImpl(b);
}
```
Which has [this](https://github.com/dotnet/runtime/blob/955cdca0ab7c1d40852d41448f8fa129c07b61a4/src/coreclr/System.Private.CoreLib/src/System/MulticastDelegate.CoreCLR.cs#L212-L294) implementation.
MulticastDelegate internally contains  _invocationList field, which is a list of delegates to be invoked.
```csharp
private object? _invocationList; // Initialized by VM as needed
private nint _invocationCount;
```
`CombineImpl` method extracts the follow's invocation list:
```csharp
object[]? followList = dFollow._invocationList as object[];
int followCount = (followList != null) ? dFollow._invocationCount : 1;
```
1 - case: `this` is a single delegate(meaning it doesn't contain chained methods):
```csharp
if (_invocationList is not object[] invocationList)
```
In this case, implementation is trivial, the first index is `this` delegate, and 1 + index is the `followList` delegates(if any):
```csharp
resultCount = 1 + followCount;
resultList = new object[resultCount];
resultList[0] = this;
if (followList == null)
{ 
    resultList[1] = dFollow;
}
else
{
    for (int i = 0; i < followCount; i++)
        resultList[1 + i] = followList[i];
}
return NewMulticastDelegate(resultList, resultCount);
```
2 - case: `this` is multicast(contains chained delegates). In this case, we check if `this` invocationList contains enough Length
to store both `this` and `dFollow` delegates. If yes, the subcases are covered:
* `followList`(chained delegates of the follow) is null - meaning it contains no chained delegates. In this case, the following block
is executed:
```csharp
resultList = invocationList;
if (followList == null)
{
    if (!TrySetSlot(resultList, invocationCount, dFollow))
        resultList = null;
}
```
`TrySetSlot` method is special here. It tries to set the `dFollow`(the one to be added) delegate to the `invocationList` array at the
position starting from invocationCount. If the slot is available, the `dFollow` is added to the `resultList` and the method return true
immediately. If the slot is not available(meaning the array already added the `dFollow` delegate or the given is already busy), 
the delegate is checked if it was already added, and if yes, return `true`, otherwise `false`, which means creating new array is required.
```csharp
if (a[index] is object ai)
{
    MulticastDelegate d = (MulticastDelegate)o;
    MulticastDelegate dd = (MulticastDelegate)ai;

    if (dd._methodPtr == d._methodPtr &&
        dd._target == d._target &&
        dd._methodPtrAux == d._methodPtrAux)
    {
        return true;
    }
}
```
If we couldn't add the `dFollow` delegate to the `invocationList` array, we set `resultList` to null.

* `followList`(chained delegates of the follow) is not null - meaning it contains chained delegates. In this case, we try to insert
each delegate from `dFollow` to the `resultList`(remember it is set to invocationList) array starting from the position of invocationCount,
and if at least one delegate couldn't be added, we break the loop and set `resultList` to null:
```csharp
else
{
    for (int i = 0; i < followCount; i++)
    {
        if (!TrySetSlot(resultList, invocationCount + i, followList[i]))
        {
            resultList = null;
            break;
        }
    }
}
```
If at any point we couldn't reuse `invocationList` array or at any point couldn't add `dFollow` or its delegates, we create a new resulting array
double the size of the invocationList array and add both `this` and `dFollow` delegates to it manually:
```csharp
int allocCount = invocationList.Length;
while (allocCount < resultCount)
    allocCount *= 2;

resultList = new object[allocCount];

//the rest is just copying...
```
### Remove
Remove method internally calls `RemoveImpl` method:
```csharp
public static Delegate? Remove(Delegate? source, Delegate? value) 
{
    return source.RemoveImpl(value);
}
```
In `RemoveImpl` method, we check if the `value` contains chained delegates:
* If no, we check if `this` contains any chained delegates. 
    * If no, we check if `this` and `value` delegates are equal. If yes, just return null. It works as follows:
    ```csharp
     Action a = A;
     Action b = A;
     
     a -= b; // a is null now
    ```
    * If yes, for all  chained delegates of `this` delegate, compare them with `value` delegate. 
