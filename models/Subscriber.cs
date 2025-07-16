public class Subscriber
{
    public string SubscriberID { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
}
