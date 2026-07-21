## Dictionary
Dictionary is a collection of `key-value` pairs. Main advantage is that it provides `O(1)` constant time performance for lookups,
insertion and deletion operations. It uses a `hash function` to map keys to indices in an array, allowing for fast retrieval of values based on their associated keys.
For example, consider the following code:
```csharp
Dictionary<char, int> freqMap = new Dictionary<char, int>();
string word = "The cat sleeps.";
for (int i = 0; i < word.Length; i++)
{
    if (freqMap.ContainsKey(word[i]))
    {
        freqMap[word[i]]++;
    }
    else
    {
        freqMap.Add(word[i], 1);
    }
}

Console.WriteLine("Frequency of each character:");
foreach (var item in freqMap)
{
    Console.WriteLine($"{item.Key}: {item.Value}");
}
```
Here, we use `char` type as a key, and `int` as a value. Basically, this code counts the frequency of each character occured in 
`word` and prints them accordingly. Any data type could be used for both key and values.

## Internals
As was mentioned earlier, a `hash function` is used to map keys to indices in an array, which is used internally to efficiently
store and retrieve the value based on the key. If we look at the [source code](https://github.com/dotnet/dotnet/blob/c506080fe921205a9b2b374ed3fd37b297c6d74a/src/runtime/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs#L111),
the first thing we see is the following fields:
```csharp
private int[]? _buckets;
private Entry[]? _entries;
#if TARGET_64BIT
        private ulong _fastModMultiplier;
#endif
private int _count;
private int _freeList;
private int _freeCount;
private int _version;
private IEqualityComparer<TKey>? _comparer;
private KeyCollection? _keys;
private ValueCollection? _values;
private const int StartOfFreeList = -3;
```
What we are interested in here, are the fields `_buckets` and `_entries`. `_buckets` is an array of integers, which stores the 
index of the first entry inside `_entries` array. `_entries` is an array of `Entry` structs, where every elements stores the actual
data. Let's consider the rest of the fields:
* `_count` - represents how many entries have been allocated.
* `_freeList` - removed items aren't physically removed, but their slots become reusable, this stores the index of the next reusable slot(index to `_entries`).
* `_freeCount` - tracks how many free entries exist in total
* `_version` - is needed to track state during enumeration.
* `_comparer` - controls how keys would be compared and hashed
* `_keys` - needed for fast access to dictionary.Keys instead of creating new object every time
* `_values` - needed for fast access to dictionary.Values
* `StartOfFreeList` - when entry becomes free, its `next` field is repurposed to build a linked-list of free slots. It represents
index 0 by encoding the value as -3. For example: `next >= -1` indicates normal collision chain, and `next < -1` indicates
the entry is on the free list. -3 represents index 0 on the free list, -4 represents index 1 on the free list, -1 indicates
the end of free list.

Then, we see the constructor implementation:
```csharp
public Dictionary(int capacity, IEqualityComparer<TKey>? comparer)
{
    if (capacity < 0)
    {
        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
    }

    if (capacity > 0)
    {
        Initialize(capacity);
    }

    // For reference types, we always want to store a comparer instance, either
    // the one provided, or if one wasn't provided, the default (accessing
    // EqualityComparer<TKey>.Default with shared generics on every dictionary
    // access can add measurable overhead).  For value types, if no comparer is
    // provided, or if the default is provided, we'd prefer to use
    // EqualityComparer<TKey>.Default.Equals on every use, enabling the JIT to
    // devirtualize and possibly inline the operation.
    if (!typeof(TKey).IsValueType)
    {
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        // Special-case EqualityComparer<string>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
        // We use a non-randomized comparer for improved perf, falling back to a randomized comparer if the
        // hash buckets become unbalanced.
        if (typeof(TKey) == typeof(string) &&
            NonRandomizedStringEqualityComparer.GetStringComparer(_comparer!) is IEqualityComparer<string> stringComparer)
        {
            _comparer = (IEqualityComparer<TKey>)stringComparer;
        }
    }
    else if (comparer is not null && // first check for null to avoid forcing default comparer instantiation unnecessarily
        comparer != EqualityComparer<TKey>.Default)
    {
                _comparer = comparer;
    }
}
```
Here, we ensure the capacity is larger than 0, and if it is, will call the following method:
```csharp
private int Initialize(int capacity)
{
    int size = HashHelpers.GetPrime(capacity);
    int[] buckets = new int[size];
    Entry[] entries = new Entry[size];

    // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
    _freeList = -1;
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
#endif
_buckets = buckets;
_entries = entries;
return size;
}
```

`GetPrime` internal method is trivial:
```csharp
// Table of prime numbers to use as hash table sizes.
// A typical resize algorithm would pick the smallest prime number in this array
// that is larger than twice the previous capacity.
// Suppose our Hashtable currently has capacity x and enough elements are added
// such that a resize needs to occur. Resizing first computes 2x then finds the
// first prime in the table greater than 2x, i.e. if primes are ordered
// p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
// Doubling is important for preserving the asymptotic complexity of the
// hashtable operations such as add.  Having a prime guarantees that double
// hashing does not lead to infinite loops.  IE, your hash function will be
// h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
// We prefer the low computation costs of higher prime numbers over the increased
// memory allocation of a fixed prime number i.e. when right sizing a HashSet.
internal static ReadOnlySpan<int> Primes =>
    [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];

public const int HashPrime = 101;
public static int GetPrime(int min)
{
    if (min < 0)
                throw new ArgumentException(SR.Arg_HTCapacityOverflow);

    foreach (int prime in Primes)
    {
        if (prime >= min)
            return prime;
    }

    // Outside of our predefined table. Compute the hard way.
    for (int i = (min | 1); i < int.MaxValue; i += 2)
    {
        if (IsPrime(i) && ((i - 1) % HashPrime != 0))
            return i;
    }
    return min;
}
```
The code is self-descriptive, but in short, what `GetPrime` does is it finds the minimum prime number which is `>=min`.
It uses `Primes` so that manual lookup of primes was not performed, but `Primes` only bound to 7199369, so we are asking
for capacity larger than that, we will need to compute in the hard way(manually, without a table).

After we computed the relevant size for the capacity, we initialize the `_buckets` and `_entries` variables with arrays of 
this size. After initialization, `_comparer` part is self-descriptive, but we are interested in special case when a key's type
used in dictionary is `string`:
```csharp
if (typeof(TKey) == typeof(string) &&
            NonRandomizedStringEqualityComparer.GetStringComparer(_comparer!) is IEqualityComparer<string> stringComparer)
{
    _comparer = (IEqualityComparer<TKey>)stringComparer;
}
```
Inside `NonRandomizedStringEqualityComparer`, we see the following code:
```csharp
// Dictionary<...>.Comparer and similar methods need to return the original IEqualityComparer
// that was passed in to the ctor. The caller chooses one of these singletons so that the
// GetUnderlyingEqualityComparer method can return the correct value.
private static readonly NonRandomizedStringEqualityComparer WrappedAroundDefaultComparer = new OrdinalComparer(EqualityComparer<string?>.Default);
private static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinal = new OrdinalComparer(StringComparer.Ordinal);
private static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinalIgnoreCase = new OrdinalIgnoreCaseComparer(StringComparer.OrdinalIgnoreCase);
     
public static IEqualityComparer<string>? GetStringComparer(object comparer)
{
    // Special-case EqualityComparer<string>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
    // We use a non-randomized comparer for improved perf, falling back to a randomized comparer if the
    // hash buckets become unbalanced.

    if (ReferenceEquals(comparer, EqualityComparer<string>.Default))
    {
        return WrappedAroundDefaultComparer;
    }

    if (ReferenceEquals(comparer, StringComparer.Ordinal))
    {
        return WrappedAroundStringComparerOrdinal;
    }

    if (ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
    {
        return WrappedAroundStringComparerOrdinalIgnoreCase;
    }

    return null;
}
```
* `NonRandomizedStringEqualityComparer` is the "fast/non-randomized" hashing/equality behaviour. It is optimistic in terms of
performance, but is vulnerable to an attack called [hash-flooding](https://v8.dev/blog/hash-flooding). The fallback mechanism
mentioned in comments, means that every new `string-keyed Dictionary` starts out using `non-randomized` hashing (fast).
The dictionary internally watches how long its collision chains (number of keys stuffed in one bucket) get. If a bucket's
chain gets suspiciously long — a strong signal of either a hash-flooding attack or just terrible luck — the dictionary
silently switches itself over to `randomized` hashing and rehashes everything.
One might wonder: What's the point in this compare and swap mechanism? Because we want ordinal semantics(case-sensitive,
non-culture-aware), but we want them with non-randomized hashing. Notice that all wrapper objects are `singleton` objects, 
because their behaviour is fixed and universal.

The second condition inside the Dictionary ctor is this:
```csharp
else if (comparer is not null && // first check for null to avoid forcing default comparer instantiation unnecessarily
    comparer != EqualityComparer<TKey>.Default)
{
    _comparer = comparer;
}
```
Notice that we explicitly set `_comparer` as null if `EqualityComparer<TKey>.Default` is passed as an argument.
The answer is that `_comparer == null` becomes a meaningful signal used throughout the rest of
Dictionary's internals: "this dictionary is using default comparison behavior."

There is another overload of Dictionary ctor that has the following implementation:
```csharp
public Dictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer) :
    this(dictionary?.Count ?? 0, comparer)
{
    if (dictionary == null)
    {
        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
    }

    AddRange(dictionary);
}
```
This is the constructor that lets you build a Dictionary by copying another dictionary. Let's look at what's inside the
`AddRange` method, in particular the following block:
```csharp
if (enumerable.GetType() == typeof(Dictionary<TKey, TValue>))
{
    Dictionary<TKey, TValue> source = (Dictionary<TKey, TValue>)enumerable;
    // the branch when comparers don't match
    // Comparers differ need to rehash all the entries via Add
    int count = source._count;
    for (int i = 0; i < count; i++)
    {
        // Only copy if an entry
        if (oldEntries[i].next >= -1)
        {
            Add(oldEntries[i].key, oldEntries[i].value);
        }
    }
    return;
}
```
As mentioned earlier, `next >= -1` indicates live entry( index of the next entry in the same bucket's collision
chain (a value >= 0), or -1 if it's the last (or only) entry in its chain). For each such entry, `Add` method is called:
```csharp
public void Add(TKey key, TValue value)
{
    bool modified = TryInsert(key, value, InsertionBehavior.ThrowOnExisting);
    Debug.Assert(modified); // If there was an existing key and the Add failed, an exception will already have been thrown.
}
```
Let's dig into how `TryInsert` is implemented, branch-by-branch.
First branch:
```csharp
private bool TryInsert(TKey key, TValue value, InsertionBehavior behavior)
{
    // NOTE: this method is mirrored in CollectionsMarshal.GetValueRefOrAddDefault below.
    // If you make any changes here, make sure to keep that version in sync as well.
    if (key == null)
    {
        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
    }

    if (_buckets == null)
    {
        Initialize(0);
    }
    Debug.Assert(_buckets != null);

    Entry[]? entries = _entries;
    Debug.Assert(entries != null, "expected entries to be non-null");

    IEqualityComparer<TKey>? comparer = _comparer;
    Debug.Assert(comparer is not null || typeof(TKey).IsValueType);
    uint hashCode = (uint)((typeof(TKey).IsValueType && comparer == null) ? key.GetHashCode() : comparer!.GetHashCode(key));

    uint collisionCount = 0;
    ref int bucket = ref GetBucket(hashCode);
    int i = bucket - 1; // Value in _buckets is 1-based

    if (typeof(TKey).IsValueType && // comparer can only be null for value types; enable JIT to eliminate entire if block for ref types
        comparer == null)
    {
        // ValueType: Devirtualize with EqualityComparer<TKey>.Default intrinsic
        while ((uint)i < (uint)entries.Length)
        {
            if (entries[i].hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entries[i].key, key))
            {
                if (behavior == InsertionBehavior.OverwriteExisting)
                {
                    entries[i].value = value;
                    return true;
                }

                if (behavior == InsertionBehavior.ThrowOnExisting)
                {
                    ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                }

                return false;
            }

            i = entries[i].next;

            collisionCount++;
            if (collisionCount > (uint)entries.Length)
            {
                // The chain of entries forms a loop; which means a concurrent update has happened.
                // Break out of the loop and throw, rather than looping forever.
                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
            }
        }
    }
}
```
From this code, it is visible that a freshly-constructed empty Dictionary doesn't allocate `_buckets/_entries`
until the first insertion.