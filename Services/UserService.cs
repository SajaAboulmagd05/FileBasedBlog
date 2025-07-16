

using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

public class UserService
{
    public async Task<IResult> RegisterUser(HttpRequest request)
    {
        var form = await request.ReadFormAsync();
        var name = form["name"].ToString().Trim();
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var confirm = form["confirmPassword"].ToString();
        var wantsNewsletter = form["newsletter"] == "on";

        // Basic validation
        if (string.IsNullOrWhiteSpace(name) || !IsValidEmail(email))
            return Results.BadRequest("Invalid name or email.");

        if (password != confirm || password.Length < 6)
            return Results.BadRequest("Passwords must match and be at least 6 characters.");

        if (UserExists(email))
            return Results.BadRequest("This email is already registered.");

        // Hash password
        var hashed = BCrypt.Net.BCrypt.HashPassword(password);

        // Generate verification token
        var token = Guid.NewGuid().ToString();
        SaveVerificationToken(email, token);

        // Create user object
        var user = new UserProfile
        {
            Name = name,
            Email = email,
            PasswordHash = hashed,
            Role = "Member",
            IsSubscribedToNewsletter = wantsNewsletter,
            IsEmailVerified = false
        };

        SaveUser(user);

        // Send email
        await SendVerificationEmail(email, name, token);

        return Results.Ok("User registered. Check your inbox to verify.");
    }

    private bool IsValidEmail(string email) =>
     Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

    public async Task SendVerificationEmail(string email, string name, string token)
    {
        var link = $"http://localhost:5000/api/verify?email={Uri.EscapeDataString(email)}&token={token}";
        var body = $"""
    Hi {name},

    Thanks for registering!

    Please verify your email by clicking here: <a href="{link}">Verify Email</a>

    Cheers,  
    My Blog Team
    """;

        using var client = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("sajablog9@gmail.com", "xktgxjkvxzsmnfyo"),
            EnableSsl = true
        };

        var message = new MailMessage("sajablog9@gmail.com", email)
        {
            Subject = "Verify your email",
            Body = body,
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }


    public void SaveUser(UserProfile user)
    {
        var dirName = Regex.Replace(user.Email.ToLower(), @"[@\.]", "_");
        // e.g., sajanabil74@gmail.com → sajanabil74_gmail_com

        var userDir = Path.Combine("content", "users", dirName);
        Directory.CreateDirectory(userDir);

        var profilePath = Path.Combine(userDir, "Profile.json");
        var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(profilePath, json);
    }
    public bool UserExists(string email)
    {
        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var userDir = Path.Combine("content", "users", dirName);
        var profilePath = Path.Combine(userDir, "Profile.json");

        return File.Exists(profilePath);
    }


    public void SaveVerificationToken(string email, string token)
    {
        var verificationDir = Path.Combine("content", "verification");
        Directory.CreateDirectory(verificationDir);

        var tokenFile = Path.Combine(verificationDir, "pending.json");

        Dictionary<string, string> tokenMap;

        if (File.Exists(tokenFile))
        {
            var raw = File.ReadAllText(tokenFile);
            tokenMap = JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? new();
        }
        else
        {
            tokenMap = new Dictionary<string, string>();
        }

        // Overwrite or add new token
        tokenMap[email] = token;

        var updatedJson = JsonSerializer.Serialize(tokenMap, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tokenFile, updatedJson);
    }

}