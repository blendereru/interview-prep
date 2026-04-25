namespace Events_Concepts;

public class InventoryService
{
    public async Task OnOrderShipped(Guid orderId)
    {
        await Task.Delay(1000);
        Console.WriteLine("Inventory reducted: {0}", orderId);
    }
}