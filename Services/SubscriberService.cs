// will probably remove it 


using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

public class SubscriberService
{
    private readonly string subscriberFilePath = "content/subscribers/subscribers.json";

    public List<Subscriber> GetAll()
    {
        if (!File.Exists(subscriberFilePath))
            return new List<Subscriber>();

        var json = File.ReadAllText(subscriberFilePath);
        return JsonSerializer.Deserialize<List<Subscriber>>(json) ?? new List<Subscriber>();
    }

    public void Add(Subscriber newSubscriber)
    {
        var subscribers = GetAll();

        if (subscribers.Any(s => s.Email.Equals(newSubscriber.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Email is already subscribed.");

        subscribers.Add(newSubscriber);

        var json = JsonSerializer.Serialize(subscribers, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(subscriberFilePath, json);
    }
    public async Task<IResult> HandleSubscription(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            return Results.BadRequest("Invalid email address.");

        if (IsSubscribed(email))
            return Results.BadRequest("Email is already subscribed.");

        var subscriber = new Subscriber
        {
            Email = email,
            SubscribedAt = DateTime.UtcNow
        };

        Add(subscriber);

        try
        {
            await SendWelcomeEmail(email, subscriber.SubscriberID);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Subscription saved, but failed to send email: {ex.Message}");
        }

        return Results.Ok("Subscription successful.");
    }


    public bool Remove(string email)
    {
        var subscribers = GetAll();
        var updated = subscribers.RemoveAll(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) > 0;

        if (updated)
        {
            var json = JsonSerializer.Serialize(subscribers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(subscriberFilePath, json);
        }

        return updated;
    }

    public bool IsSubscribed(string email)
    {
        var subscribers = GetAll();
        return subscribers.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }
    private async Task SendWelcomeEmail(string toEmail, string subscriberID)
    {
        var from = "sajablog9@gmail.com";
        var subject = "Welcome to My Blog!";
        var unsubscribeLink = $"http://localhost:5000/api/unsubscribe?email={Uri.EscapeDataString(toEmail)}";


        using var client = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("sajablog9@gmail.com", "xktgxjkvxzsmnfyo"),
            EnableSsl = true
        };

        var message = new MailMessage(from, toEmail)
        {
            Subject = subject,
            Body = $"""
        <p>Hi there!</p>

        <p>Thanks for subscribing to <strong>My Blog</strong>. We'll notify you whenever new content drops!</p>

        <p>If you'd like to unsubscribe, click here: <a href="{unsubscribeLink}">unsubscribe</a></p>

        <p>Cheers,<br />My Blog Team</p>
    """,
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("SMTP error: " + ex.Message);
            throw;
        }

    }



    private bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

}
