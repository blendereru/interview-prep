## Events
Events are basically the same as delegates, point to a method(or bunch of methods), and calls them when the event is triggered. They are
defined using the `event` keyword. The convention is as follows:
```
event <delegate_type> <event_name>
```
Delegate types are defined using the `delegate` keyword, and events point to methods that match the delegate type. So why then events
are needed at all, if we have delegates that do the same thing? Events can't be directly invoked from outside the class,
where it is defined(even through they are public). This is because events support the `publisher` and `subscriber` pattern.
`Publisher` is the class that defines the event, and that decides when to invoke the event. `Subscriber` is the class that eventually 
subscribes to the event and publisher triggers it when invoked. `Publisher` decides when to invoke the event, and `subscriber`
decides what to do with the event, and that is what differs events from delegates. More about decision making between delegates
and events is [here](https://learn.microsoft.com/en-us/answers/questions/1163467/c-events-and-delegates-when-should-i-take-what).

The best way to think of events is as an encapsulation of a delegate, as only publisher itself can invoke the event, reset it, and 
control when it fires. Events have the `add` and `remove` accessors, which are used to subscribe and unsubscribe from the event, and through
them, we can define our custom behaviour on the subscribers. For example, in the `OrderService` class, we can restrict that
`EmailService` can't be subscribed to the `OrderPlaced` event(it can, but we explicitly disallow it):
```csharp
private OrderPlacedHandler? _orderPlaced;

public event OrderPlacedHandler? OrderPlaced
{
    add
    {
        if (value?.Target is EmailService)
        {
            return;
        }

        _orderPlaced += value;
    }
    
    remove
    {
        _orderPlaced -= value;
    }
}
```
`add` is called when the subscriber subscribes to the event through the `+=` operator, and `remove` is called when the subscriber unsubscribes
from the event through the `-=` operator. But when we define the event with custom accessors, we can't invoke the event directly,
we have to invoke the private delegate, and event itself just becomes a wrapper around it. In fact, every event is just a wrapper around
a delegate, we just don't see them. For our `OrderPlaced` event, for instance, compiler generates the following code:
```csharp
[CompilerGenerated]
[DebuggerBrowsable(DebuggerBrowsableState.Never)]
private OrderPlacedHandler? m_OrderPlaced;

public event OrderPlacedHandler? OrderPlaced
{
    [CompilerGenerated]
    add
    {
        OrderPlacedHandler orderPlacedHandler = this.m_OrderPlaced;
        OrderPlacedHandler orderPlacedHandler2;
        do
        {
            orderPlacedHandler2 = orderPlacedHandler;
            OrderPlacedHandler value2 = (OrderPlacedHandler)Delegate.Combine(orderPlacedHandler2, value);
            orderPlacedHandler = Interlocked.CompareExchange(ref this.m_OrderPlaced, value2, orderPlacedHandler2);
        }
        while ((object)orderPlacedHandler != orderPlacedHandler2);
    }
    [CompilerGenerated]
    remove
    {
        OrderPlacedHandler orderPlacedHandler = this.m_OrderPlaced;
        OrderPlacedHandler orderPlacedHandler2;
        do
        {
            orderPlacedHandler2 = orderPlacedHandler;
            OrderPlacedHandler value2 = (OrderPlacedHandler)Delegate.Remove(orderPlacedHandler2, value);
            orderPlacedHandler = Interlocked.CompareExchange(ref this.m_OrderPlaced, value2, orderPlacedHandler2);
        }
        while ((object)orderPlacedHandler != orderPlacedHandler2);
    }
}
```
In the `add` block, `value2` combines the event's delegate and value, and then atomically checks if the event's delegate didn't 
change after combination, and if it didn't, it replaces the event's delegate with the combined one. If it did, it means
that the event's delegate changed(because another thread subscribed to the event before us), so we need to retry the combination.
This is an algorithm that ensures that during concurrent delegates combination, no delegate is lost in midway. Consider an example:
During `Combine` operation, two threads try to write their values:
* Thread A: Combine(A, B)
* Thread B: Combine(A, C)
Thread A atomically checks if the event delegate(A just holds reference to it) didn't change after combining A and B, `CompareExchange` returns A.
While loop returns true, so `Combine` succeded. Thread B at the same time checks if the event delegate didn't change after combining A and C,
since it changed after thread A combined A and B, `CompareExchange` returns A + B. At second iteration(since while loop returns false),
the block `do` recomputes the reference to the event delegate, and `Combine` is called again, now with the A + B + C. So that's how
the logic ensures to handle concurrent delegates combination. The logic is the same for `Remove` operation.