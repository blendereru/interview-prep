namespace Events_Concepts;

public class EmailService
{
    public async Task OnOrderPlaced(Guid orderId)
    {
        await Task.Delay(1000);
        Console.WriteLine("Email on order being placed is sent to user email: {0}", orderId);
    }
    
    public async Task OnOrderShipped(Guid orderId)
    {
        await Task.Delay(1000);
        Console.WriteLine("Email on order being shipped is sent to user email: {0}", orderId);
    }
}