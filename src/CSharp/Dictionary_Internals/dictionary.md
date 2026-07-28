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

There is another overload of `Dictionary` ctor that has the following implementation:
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
As mentioned earlier, `next >= -1` indicates live entry (index of the next entry in the same bucket's collision
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

                return false; // dict.TryAdd(key, value) → report "nope, already there," no throw, no overwrite
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
until the first insertion. The following block is interesting:
```csharp
uint hashCode = (uint)((typeof(TKey).IsValueType && comparer == null) ? key.GetHashCode() : comparer!.GetHashCode(key));
```
Because we know `TKey` is not a reference type we could directly call `GetHashCode` on the value type itself, as calling
the method on `comparer` instance boxes the argument. The retrieved `hashCode` is then passed to `GetBucket`, which has 
the following implementation:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private ref int GetBucket(uint hashCode)
{
    int[] buckets = _buckets!;
#if TARGET_64BIT
    return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
#else
    return ref buckets[(uint)hashCode % buckets.Length];
#endif
}
```
This basically maps the `hashCode` to the correct slot in `_buckets`. Let's enter the first branch with the following loop:
```csharp
while ((uint)i < (uint)entries.Length)
```
Casting to `uint` is a trick, so that when `i = -1`, the cast produced value `4,294,967,295` which is larger than `entries.Length`,
so the single cast covers both "index is within bounds" and "index isn't the -1 sentinel" in one comparison.
```csharp
if (entries[i].hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entries[i].key, key))
```
This check ensures `entries[i]` is a genuine existing entry with the exact same key we are trying to insert(meaning this key
is already in the dictionary). Depending on the `InsertionBehaviour`, we override the value or not.

If no match happened, move on to the next entry in this bucket's collision chain:
```csharp
i = entries[i].next;
```
The `else` block is basically the same, except for `EqualityComparer<TKey>.Default` intrinsic, so we will not consider it here.
Let's consider the actual insertion logic:
```csharp
int index;
if (_freeCount > 0)
{
    index = _freeList;
    Debug.Assert((StartOfFreeList - entries[_freeList].next) >= -1, "shouldn't overflow because `next` cannot underflow");
    _freeList = StartOfFreeList - entries[_freeList].next;
    _freeCount--;
}
else
{
    int count = _count;
    if (count == entries.Length)
    {
        Resize();
        bucket = ref GetBucket(hashCode);
    }
    index = count;
    _count = count + 1;
    entries = _entries;
}

ref Entry entry = ref entries![index];
entry.hashCode = hashCode;
entry.next = bucket - 1; // Value in _buckets is 1-based
entry.key = key;
entry.value = value;
bucket = index + 1; // Value in _buckets is 1-based
_version++;

// Value types never rehash
if (!typeof(TKey).IsValueType && collisionCount > HashHelpers.HashCollisionThreshold && comparer is NonRandomizedStringEqualityComparer)
{
    // If we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
    // i.e. EqualityComparer<string>.Default.
    Resize(entries.Length, true);
}

return true;
```
When `_freeCount > 0`, `index = _freeList` grabs the head of the free list as the slot for the new entry. Then the free 
list needs to advance to point to the next free slot. When there is no free slot(`else` block) - indicating the array is 
completely full - it's time to grow the array, via the `Resize()`:
```csharp
private void Resize() => Resize(HashHelpers.ExpandPrime(_count), false);

private void Resize(int newSize, bool forceNewHashCodes)
{
    // Value types never rehash
    Debug.Assert(!forceNewHashCodes || !typeof(TKey).IsValueType);
    Debug.Assert(_entries != null, "_entries should be non-null");
    Debug.Assert(newSize >= _entries.Length);

    Entry[] entries = new Entry[newSize];

    int count = _count;
    Array.Copy(_entries, entries, count);

    if (!typeof(TKey).IsValueType && forceNewHashCodes)
    {
        Debug.Assert(_comparer is NonRandomizedStringEqualityComparer);
        IEqualityComparer<TKey> comparer = _comparer = (IEqualityComparer<TKey>)((NonRandomizedStringEqualityComparer)_comparer).GetRandomizedEqualityComparer();

        for (int i = 0; i < count; i++)
        {
            if (entries[i].next >= -1)
            {
                entries[i].hashCode = (uint)comparer.GetHashCode(entries[i].key);
            }
        }
    }

    // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
    _buckets = new int[newSize];
#if TARGET_64BIT
    _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
#endif
    for (int i = 0; i < count; i++)
    {
        if (entries[i].next >= -1)
        {
            ref int bucket = ref GetBucket(entries[i].hashCode);
            entries[i].next = bucket - 1; // Value in _buckets is 1-based
            bucket = i + 1;
        }
    }

    _entries = entries;
}
```
Parameterless `Resize()` method calls the overload method with `ExpandPrime(_count)` and `forceNewHashCodes` set to `false`.
Now, this is fed into second `Resize(int, bool)` method declaration, where a new, bigger array is allocated, and the existing
live+free entries (up to the high-water mark count) are bulk-copied across via `Array.Copy` — this preserves each entry's `key`,
`value`, and cached hash code as raw data for now.
Let's consider what's inside `ExpandPrime`:
```csharp
// This is the maximum prime smaller than Array.MaxLength.
public const int MaxPrimeArrayLength = 0x7FFFFFC3;

// Returns size of hashtable to grow to.
public static int ExpandPrime(int oldSize)
{
    int newSize = 2 * oldSize;

    // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
    // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
    if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
    {
        Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
        return MaxPrimeArrayLength;
    }

    return GetPrime(newSize);
}
```
The logic is trivial: double the size of the entries size, and round this `newSize` to the next prime number at or above that value.
```csharp
if (!typeof(TKey).IsValueType && forceNewHashCodes)
{
    Debug.Assert(_comparer is NonRandomizedStringEqualityComparer);
    IEqualityComparer<TKey> comparer = _comparer = (IEqualityComparer<TKey>)((NonRandomizedStringEqualityComparer)_comparer).GetRandomizedEqualityComparer();

    for (int i = 0; i < count; i++)
    {
        if (entries[i].next >= -1)
        {
            entries[i].hashCode = (uint)comparer.GetHashCode(entries[i].key);
        }
    }
}
```
The following logic assumes that `hash-flooding attack` happened, so we are applying `randomized string hashing` instead.
Inside `GetRandomizedEqualityComparer`, `RandomizedStringEqualityComparer` having its own, separately-defined nested subclasses
that happen to share the same names, because they serve the exact same conceptual role (ordinal vs. ordinal-ignore-case)
just implemented with `randomized` hashing logic instead of `non-randomized`:
```csharp
internal virtual RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
{
    return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: false);
}

internal static RandomizedStringEqualityComparer Create(IEqualityComparer<string?> underlyingComparer, bool ignoreCase)
{
    if (!ignoreCase)
    {
        return new OrdinalComparer(underlyingComparer);
    }
    else
    {
        return new OrdinalIgnoreCaseComparer(underlyingComparer);
    }
}
```

Consider the code below:
```csharp
for (int i = 0; i < count; i++)
{
    if (entries[i].next >= -1)
    {
        entries[i].hashCode = (uint)comparer.GetHashCode(entries[i].key);
    }
}
```
This overwrites the cached hash code stored in the entry, recomputing it using the `newly-randomized` comparer instead
of whatever the old `non-randomized` comparer had produced. Then, the code:
```csharp
// Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
_buckets = new int[newSize];
#if TARGET_64BIT
    _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
#endif
    for (int i = 0; i < count; i++)
    {
        if (entries[i].next >= -1)
        {
            ref int bucket = ref GetBucket(entries[i].hashCode);
            entries[i].next = bucket - 1; // Value in _buckets is 1-based
            bucket = i + 1;
        }
    }
    _entries = entries;
```
Expands the `_buckets`(as well as `entries`). Then:
```csharp
ref int bucket = ref GetBucket(entries[i].hashCode);
```
The block above, returns `bucket` value by reference given the `hashCode`(which is 0 when retrieved, before write, as array
was expanded, it is brand-new). The loop then re-links every `live` entry into its (possibly new) bucket chain, since
bucket placement depends on `newSize` and must be rebuilt from scratch regardless of whether hash codes themselves changed.

This was just one of the few branches of `AddRange` method. Let's consider another branch:
```csharp
// We similarly special-case KVP<>[] and List<KVP<>>, as they're commonly used to seed dictionaries, and
// we want to avoid the enumerator costs (e.g. allocation) for them as well. Extract a span if possible.
ReadOnlySpan<KeyValuePair<TKey, TValue>> span;
if (enumerable is KeyValuePair<TKey, TValue>[] array)
{
    span = array;
}
else if (enumerable.GetType() == typeof(List<KeyValuePair<TKey, TValue>>))
{
    span = CollectionsMarshal.AsSpan((List<KeyValuePair<TKey, TValue>>)enumerable);
}
else
{
    // Fallback path for all other enumerables
    foreach (KeyValuePair<TKey, TValue> pair in enumerable)
    {
        Add(pair.Key, pair.Value);
    }
    return;
}

// We got a span. Add the elements to the dictionary.
foreach (KeyValuePair<TKey, TValue> pair in span)
{
    Add(pair.Key, pair.Value);
}
```

Then, we have `Comparer` property with accessors:
```csharp
public IEqualityComparer<TKey> Comparer
{
    get
    {
        if (typeof(TKey) == typeof(string))
        {
            Debug.Assert(_comparer is not null, "The comparer should never be null for a reference type.");
            return (IEqualityComparer<TKey>)IInternalStringEqualityComparer.GetUnderlyingEqualityComparer((IEqualityComparer<string?>)_comparer);
        }
        else
        {
            return _comparer ?? EqualityComparer<TKey>.Default;
        }
    }
}
```
Checking the `TKey` if of type `string` is reasonable because of the `ctor` logic we covered before: if `_comparer` is one of
the `NonRandomizedStringEqualityComparer` wrapper singletons (`WrappedAroundDefaultComparer`,
`WrappedAroundStringComparerOrdinal`, `WrappedAroundStringComparerOrdinalIgnoreCase`), this call retrieves the original
comparer object that wrapper was standing in for (`EqualityComparer<string>.Default`, `StringComparer.Ordinal`, 
or `StringComparer.OrdinalIgnoreCase` respectively) — the one the user actually passed in (or that was implied by default),
preserving the round-trip guarantee (`dict.Comparer` == `StringComparer.Ordinal` being `true`).
```csharp
internal interface IInternalStringEqualityComparer : IEqualityComparer<string?>
{
    IEqualityComparer<string?> GetUnderlyingEqualityComparer();

    /// <summary>
    /// Unwraps the internal equality comparer, if proxied.
    /// Otherwise returns the equality comparer itself or its default equivalent.
    /// </summary>
    internal static IEqualityComparer<string?> GetUnderlyingEqualityComparer(IEqualityComparer<string?> outerComparer)
    {
        if (outerComparer is IInternalStringEqualityComparer internalComparer)
        {
            return internalComparer.GetUnderlyingEqualityComparer();
        }
        else
        {
            return outerComparer;
        }
    }
}
```
Since `NonRandomizedStringEqualityComparer` implements `IInternalStringEqualityComparer`, the method is implemented as follows:
```csharp
// Gets the comparer that should be returned back to the caller when querying the
// ICollection.Comparer property. Also used for serialization purposes. 
public virtual IEqualityComparer<string?> GetUnderlyingEqualityComparer() => _underlyingComparer;
```
Accessing value by key using indexer does this:
```csharp
public TValue this[TKey key]
{
    get
    {
        ref TValue value = ref FindValue(key);
        if (!Unsafe.IsNullRef(ref value))
        {
            return value;
        }

        ThrowHelper.ThrowKeyNotFoundException(key);
        return default;
    }
    set
    {
        bool modified = TryInsert(key, value, InsertionBehavior.OverwriteExisting);
        Debug.Assert(modified);
    }
}
```
`Setter` block is trivial, let's `getter`, in particular `FindValue` method:
```csharp
internal ref TValue FindValue(TKey key)
{
    if (key == null)
    {
        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
    }

    ref Entry entry = ref Unsafe.NullRef<Entry>();
    if (_buckets != null)
    {
        Debug.Assert(_entries != null, "expected entries to be != null");
        IEqualityComparer<TKey>? comparer = _comparer;
        if (typeof(TKey).IsValueType && // comparer can only be null for value types; enable JIT to eliminate entire if block for ref types
                    comparer == null)
        {
            // TODO: Replace with just key.GetHashCode once https://github.com/dotnet/runtime/issues/117521 is resolved.
            uint hashCode = (uint)EqualityComparer<TKey>.Default.GetHashCode(key);
            int i = GetBucket(hashCode);
            Entry[]? entries = _entries;
            uint collisionCount = 0;

            // ValueType: Devirtualize with EqualityComparer<TKey>.Default intrinsic
            i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
            do
            {
                // Test in if to drop range check for following array access
                if ((uint)i >= (uint)entries.Length)
                {
                    goto ReturnNotFound;
                }

                entry = ref entries[i];
                if (entry.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.key, key))
                {
                    goto ReturnFound;
                }

                i = entry.next;

                collisionCount++;
            } while (collisionCount <= (uint)entries.Length);

            // The chain of entries forms a loop; which means a concurrent update has happened.
            // Break out of the loop and throw, rather than looping forever.
            goto ConcurrentOperation;
        }
        else
        {
            Debug.Assert(comparer is not null);
            uint hashCode = (uint)comparer.GetHashCode(key);
            int i = GetBucket(hashCode);
            Entry[]? entries = _entries;
            uint collisionCount = 0;
            i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
            do
            {
                // Test in if to drop range check for following array access
                if ((uint)i >= (uint)entries.Length)
                {
                    goto ReturnNotFound;
                }

                entry = ref entries[i];
                if (entry.hashCode == hashCode && comparer.Equals(entry.key, key))
                {
                    goto ReturnFound;
                }

                i = entry.next;

                collisionCount++;
            } while (collisionCount <= (uint)entries.Length);

            // The chain of entries forms a loop; which means a concurrent update has happened.
            // Break out of the loop and throw, rather than looping forever.
            goto ConcurrentOperation;
        }
    }

    goto ReturnNotFound;

        ConcurrentOperation:
            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
        ReturnFound:
            ref TValue value = ref entry.value;
        Return:
            return ref value;
        ReturnNotFound:
            value = ref Unsafe.NullRef<TValue>();
            goto Return;
}
```
This code looks similar to what we have seen in `TryInsert` method. The first thing we are interested at:
```csharp
ref Entry entry = ref Unsafe.NullRef<Entry>();
```
This indicates to let low-level code represent "no value found" using a `ref` variable, without needing a separate `bool
found` flag or an allocation.Similarly, at the end:
```csharp
ReturnNotFound:
    value = ref Unsafe.NullRef<TValue>();
    goto Return;
```
If the key isn't found, the method returns a "null ref" for `TValue` — the caller (elsewhere in `Dictionary`, e.g. in `TryGetValue`) is
expected to check `Unsafe.IsNullRef(ref returnedValue)` to detect "not found," rather than the method returning some sentinel
value or throwing.
```csharp
int i = GetBucket(hashCode);
```
In comparison with `TryInsert` we don't need a `ref` to bucket as we never need to write back to the bucket. Eventually, in `set` block
we are inserting the value by key but with `InsertionBehavior.OverwriteExisting` behaviour:
```csharp
set
{
    bool modified = TryInsert(key, value, InsertionBehavior.OverwriteExisting);
    Debug.Assert(modified);
}
```
`Add` method does the same as setting `kvp(Key-Value pair)` through setter above, but with different `InsertionBehaviour`:
```csharp
public void Add(TKey key, TValue value)
{
    bool modified = TryInsert(key, value, InsertionBehavior.ThrowOnExisting);
    Debug.Assert(modified); // If there was an existing key and the Add failed, an exception will already have been thrown.
}
```
