namespace Events_Concepts;

public class Order
{
    public Guid Id { get; set; }
    public DateTimeOffset? PlacedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
}