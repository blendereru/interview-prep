using Covariance_Contravariance.Models;

NotificationHandler notificationHandler = HandleAny;
notificationHandler();

AnotherNotificationHandler<Notification> anotherNotificationHandler = HandleAnyGeneric;
AnotherNotificationHandler<EmailNotification> someAnotherNotificationHandler = HandleAnyGeneric;

someAnotherNotificationHandler = anotherNotificationHandler;

//call
someAnotherNotificationHandler(new EmailNotification());
anotherNotificationHandler(new EmailNotification());

CovariantDelegate<EmailNotification> d1 = Covariant;
CovariantDelegate<Notification> d2 = d1;
CovariantDelegate<object> d3 = d1;

int a = 10;
long b = a;
//First first = ValueTypeMethod;
// first(); 

static EmailNotification HandleAny()
{
    return new EmailNotification();
}

static void HandleAnyGeneric(Notification notification)
{
    Console.WriteLine(notification.Content);
}

static EmailNotification Covariant()
{
    return new EmailNotification();
}

static int ValueTypeMethod()
{
    return 10;
}
delegate Notification NotificationHandler();
delegate void AnotherNotificationHandler<in T>(T t);
delegate T CovariantDelegate<out T>();
delegate long First();