## Access Modifiers
Access modifiers are needed to specify the visibility of a class/struct/interface/method/property/field.
Top-level types, which aren't nested inside another type, can only have `internal` or `public` access(default is `internal`).
Nested types, can have the acccessibilities as shown in the table of the following [doc](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/accessibility-levels) 

* `Public` - allows the member to be accessed without any restrictions. 
For instance:
```csharp
public class Second
    public void Member()
    {
        // can be access from outside the class        
    }
}
```
`Member` can be safely accessed from anywhere in the program, where `Second` is defined.
* `Internal` - allows the member to be accessed from within the same assembly. An `assembly` is a `.dll` or `.exe` created
by compiling one or more `.cs` files in a single compilation. In our example, `Second` also contains the `InternalMember`:
```csharp
public class Second
{
    public void Member()
    {
            
    }

    internal void InternalMember()
    {
        // only this assembly can access this member   
    }
}
```
If we called `InternalMember` from outside the assembly(from `AccessModifiers` in our example), it would throw an error, as expected:
```csharp
TestAccessModifiers.Second s = new TestAccessModifiers.Second();
s.Member(); // safe because Second is public
// s.InternalMember(); -> not safe, as different assembly
```
* `Protected` - accessible inside the containing class and by classed that inherit from it. `Third` derives from `Second`,
so it can safely access `ProtectedMember` from `Second`:
```csharp
public class Second
{
        
    protected void ProtectedMember()
    {
    }
}

public class Third : Second
{
    public void Method()
    {
        ProtectedMember(); // safe as ProtectedMember is accessible to derived classes but private for accessing
    }
}
```
Note that you can access a protected member of a base class in a derived class, only if the access occurs through the derived class type.
For example, consider the `Third` class which self-descriptive:
```csharp
public class Third : Second
{

    public void Test()
    {
        var second = new Second();
        // second.ProtectedMember();  error, not accessed through the derived class type
        Third third = new Third();
        third.ProtectedMember(); // safe, as accessed through the derived class type.
    } 
    public void Method()
    {
        ProtectedMember(); // safe as ProtectedMember is accessible to derived classes but private for accessing
    }
}
```
`protected` is part of the `protected internal` and `private protected` access modifiers.
Unlike `private protected`, the `protected` access modifier allows access from derived classes in any assembly.
Unlike `protected internal`, it doesn't allow access from non-derived classes within the same assembly.
For example, the `Ninth` class is declared in different assembly than `Second`, but it can access `ProtectedMember` from `Second`:
```csharp
public class Ninth : Second
{
    public void Test()
    {
        var ninth = new Ninth();
        ninth.ProtectedMember(); // can be accessed from any assembly
    }
}
```
What distinguishes `protected` from `private protected` is this this cross-assembly accessibility.

* `Private` - is the least permissive access level, which indicates private members can only be accessed within the class or struct
where you declare it. Consider the example:
```csharp
internal class First
{
    public void Member()
    {
        PrivateMember(); // safe as PrivateMember is getting accessed within the same class
    }

    private void PrivateMember()
    {
            
    }
}
```
`PrivateMember` only lives within the `First` class, and can't be accessed from outside the class, but it can be accessed from
nested members of the `First` class.

* `protected internal` - members can be accessed from within the same assembly through base class or a derived class 
located in another assembly can access the member only if the access occurs through a variable of the derived class type.
For example, let's look at the `Fourth` and `Sixth` classes relation:
```csharp
public class Fourth
{
    protected internal virtual int ProtectedInternalMember()
    {
        return 0;
    }
}

public class Sixth
{
    private protected virtual int PrivateProtectedMember()
    {
        Fourth fourth = new Fourth();
        return fourth.ProtectedInternalMember();
    }
}
```
`ProtectedInternalMember` is accessible from within the same assembly, so the call of it inside `Sixth` is safe.
Consider calling `Fourth` member from different assembly:
```csharp
public class Tenth : Fourth
{
    public void Test()
    {
        Tenth tenth = new Tenth();
        tenth.ProtectedInternalMember(); // safe, derived class can access through a variable of the derived type
    }
}
```
`protected internal` has its accessibility modifier changing when declaring `virtual` methods. When you define
the derived class in the same assembly as the base class, all overriden members have the same 
`protected internal` accessibility:
```csharp
public class Fourth
{
    protected internal virtual int ProtectedInternalMember()
    {
        return 0;
    }  
}
public class Eleventh : Fourth
{
    protected internal override int ProtectedInternalMember()
    {
        return base.ProtectedInternalMember();
    }
}
```
When the `Fourth` class is derived in the different assembly, overriden members have the `protected` accessibility:
```csharp
public class Fifth : Fourth
{
    protected override int ProtectedInternalMember()
    {
        return 1;
    }
}
```
This is because the `internal` modifer does not apply anymore as deriving happens in a different assembly.
* `private protected` - Accessible only within the containing class and by derived classes that are in the same assembly.
Classes deriving from base class which contains `private protected` member can not access it. 
For instance:
```csharp
public class Sixth
{
    private protected virtual int PrivateProtectedMember()
    {
        Fourth fourth = new Fourth();
        return fourth.ProtectedInternalMember();
    }
}

public class Seventh : Sixth
{
    private protected override int PrivateProtectedMember()
    {
        // Sixth sixth = new Sixth();
        // sixth.PrivateProtectedMember(); is not safe as this member can only be accessed by classes derived from Sixth
        Seventh seventh = new Seventh();
        seventh.PrivateProtectedMember(); // safe, all conditions followed
        return 3;
    }
}
```
Also, note that `InternalsVisibleTo` attribute makes `private protected` members visible to derived classes in
other assemblies. For example, let's make `AccessModifiers` assembly visible to `TestAccessModifiers` assembly:
```csharp
[assembly: InternalsVisibleTo("AccessModifiers")]
```
And access `Sixth` class `private protected` member:
```csharp
public class Twelve : Sixth
{
    public void Test()
    {
        PrivateProtectedMember();
    }
}
```
By definition of `InternalsVisibleTo` attribute, it specifies that types that are ordinarily visible only within the current assembly are visible to a specified assembly.
For that reason, compiler treats `AccessModifiers` assembly as a friend assembly, that is why the following code throws a compile-time error:
```csharp
public class Fifth : Fourth
{
    // error, should be changed to protected internal as AccessModifiers assembly is a friend assembly
    protected override int ProtectedInternalMember() 
    {
        return 1;
    }
    // safe, other assembly can derive and access this member.
}
```
Now, `Fifth` class can declare `ProtectedInteralMember` as `protected internal` as AccessModifiers assembly is a friend assembly.

## Reference
The comparison table between all `protected` modifiers difference is shown in the following [doc](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/private-protected#comparison-with-other-protected-access-modifiers)

