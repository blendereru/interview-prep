namespace Events_Concepts;

public class OrderService
{
    public delegate Task OrderPlacedHandler(Guid orderId);
    public delegate Task OrderShippedHandler(Guid orderId);

    public event OrderPlacedHandler? OrderPlaced;
    
    public event OrderShippedHandler? OrderShipped;
    
    public async Task Place(Guid orderId)
    {
        await Task.Delay(1000);
        await OrderPlaced?.Invoke(orderId)!;
    }
    
    public async Task OnOrderPlaced(Guid orderId)
    {
        await Task.Delay(1000);
        Console.WriteLine("Order placed: {0}", orderId);
    }
    
    public async Task OnOrderShipped(Guid orderId)
    {
        await Task.Delay(1000);
        Console.WriteLine("Order shipped: {0}", orderId);
    }

    public async Task Ship(Guid orderId)
    {
        await Task.Delay(1000);
        await OrderShipped?.Invoke(orderId)!;
    }
}