using Events_Concepts;

var orderService = new OrderService();
var emailService = new EmailService();
var inventoryService = new InventoryService();

var orderId = Guid.NewGuid();
orderService.OrderPlaced += orderService.OnOrderPlaced;
orderService.OrderPlaced += emailService.OnOrderPlaced;

orderService.OrderShipped += orderService.OnOrderShipped;
orderService.OrderShipped += inventoryService.OnOrderShipped;

await orderService.Place(orderId);
Thread.Sleep(3000);
await orderService.Ship(orderId);