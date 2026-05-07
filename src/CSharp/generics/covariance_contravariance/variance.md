## Variance in delegates
Variance in delegates indicate that the delegate can point to methods that accept a less derived type than the delegate or
that return a more derived type than the delegate. Consider the example:
```csharp
NotificationHandler notificationHandler = HandleAny;
notificationHandler(new EmailNotification());

static void HandleAny(Notification notification)
{
    Console.WriteLine(notification.Content);
}

delegate void NotificationHandler(EmailNotification notification);
```

`NotificationHandler` can call `HandleAny` as it accepts `Notification` as a parameter(which is a base class for `EmailNotification`).
Or the following example:
```csharp
NotificationHandler notificationHandler = HandleAny;
notificationHandler();

static EmailNotification HandleAny()
{
    return new EmailNotification();
}

delegate Notification NotificationHandler();
```
`NotificationHandler` can call `HandleAny` as it returns `EmailNotification`. This makes sense, as `NotificationHandler`
expects return type of `Notification` and hence can call any method that derives from `Notification`.

### Generic delegates
Consider the following example:
```csharp

AnotherNotificationHandler<Notification> anotherNotificationHandler = HandleAnyGeneric;
anotherNotificationHandler(new EmailNotification());

static void HandleAnyGeneric(Notification notification)
{
    Console.WriteLine(notification.Content);
}

delegate void AnotherNotificationHandler<T>(T t);
```

Generic `AnotherNotificationHandler` can call `HangleAnyGeneric` when the generic type of `T` is `Notification`. But the assignment
between generic delegates that share more and less derived types is not allowed, even if it is safe.  To enable implicit conversion
between generic delegates, use the `in` or `out` keywords. `in` keyword is only used as a type of method arguments and when you want the flexibility
to plug in more derived types. For example:
```csharp
AnotherNotificationHandler<Notification> anotherNotificationHandler = HandleAnyGeneric;
AnotherNotificationHandler<EmailNotification> someAnotherNotificationHandler = HandleAnyGeneric;

someAnotherNotificationHandler = anotherNotificationHandler;

static void HandleAnyGeneric(Notification notification)
{
    Console.WriteLine(notification.Content);
}
// Type T is declared as contravariant by using the 'in' keyword.
delegate void AnotherNotificationHandler<in T>(T t);
```
`someAnotherNotificationHandler` can be assigned to `anotherNotificationHandler` implicitly as `in` keyword is used. Think of 
`in` keyword as that delegate will consume `T`, so giving it broader consumer is always safe.

`out` keyword is used to declare that the delegate will produce `T`. It indicates the delegate is `covariant` and
enables safe substitution of more derived return types, since nothing can pass values of `T` into the delegate.
Because of this, we can assign delegate of less derived type to such delegate. Consider the example:
```csharp
CovariantDelegate<EmailNotification> d1 = Covariant;
CovariantDelegate<Notification> d2 = d1;

static EmailNotification Covariant()
{
    return new EmailNotification();
}

delegate T CovariantDelegate<out T>(); // covariant delegate with the out keyword has to return type
```
Here, the assignment of `d1` to `d2` is safe, as implicit conversion from `Notification` to `EmailNotification` is always safe.
The same is true for delegate with covariant `object` type:
```csharp
CovariantDelegate<object> d3 = d1;
```

The `Action` delegate is a covariant delegate.
```csharp
public delegate void Action<in T>(T obj)
        where T : allows ref struct;
```
`Func` delegate is both covariant and contravariant(depending on [implementation](https://github.com/dotnet/dotnet/blob/b0f34d51fccc69fd334253924abd8d6853fad7aa/src/runtime/src/libraries/System.Private.CoreLib/src/System/Function.cs) we look at):
```csharp
public delegate TResult Func<out TResult>()
        where TResult : allows ref struct;

    public delegate TResult Func<in T, out TResult>(T arg)
        where T : allows ref struct
        where TResult : allows ref struct;
```

The examples of usage of them are provided in [docs](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/covariance-contravariance/using-variance-for-func-and-action-generic-delegates)

## Constraints
* `ref`, `in`, and `out` parameters in C# can't be marked as variant.
* Combining is not supported for variant delegates, as noted in the [following](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/covariance-contravariance/variance-in-delegates#combining-variant-generic-delegates) section
* Variance for generic type parameters is supported for `reference` types only(compile-time error otherwise)