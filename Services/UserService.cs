

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


    private readonly string usersPath = Path.Combine("content", "users");
    private readonly string tokenPath = Path.Combine("content", "verification", "pending.json");

    public bool VerifyUserEmail(string email, string token)
    {
        Console.WriteLine($"[VERIFY] Attempt for {email} with token {token}");

        if (!File.Exists(tokenPath))
        {
            Console.WriteLine("[VERIFY] Token file missing.");
            return false;
        }

        var raw = File.ReadAllText(tokenPath);
        var tokenMap = JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? new();

        if (!tokenMap.TryGetValue(email, out var storedToken))
        {
            Console.WriteLine("[VERIFY] Token not found for email.");
            return false;
        }

        if (storedToken != token)
        {
            Console.WriteLine("[VERIFY] Token mismatch.");
            return false;
        }

        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var profilePath = Path.Combine(usersPath, dirName, "Profile.json");

        if (!File.Exists(profilePath))
        {
            Console.WriteLine("[VERIFY] Profile not found.");
            return false;
        }

        var userJson = File.ReadAllText(profilePath);
        var user = JsonSerializer.Deserialize<UserProfile>(userJson);
        if (user == null)
        {
            Console.WriteLine("[VERIFY] Failed to deserialize profile.");
            return false;
        }

        user.IsEmailVerified = true;
        var updatedJson = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(profilePath, updatedJson);

        tokenMap.Remove(email);
        var cleanedJson = JsonSerializer.Serialize(tokenMap, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tokenPath, cleanedJson);

        Console.WriteLine($"[VERIFY] {email} successfully verified.");
        return true;
    }

    public async Task<IResult> LoginUser(HttpRequest request, JwtService jwt)
    {
        var form = await request.ReadFormAsync();
        var email = form["loginEmail"].ToString().Trim();
        var password = form["loginPassword"].ToString();

        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var profilePath = Path.Combine("content", "users", dirName, "Profile.json");

        if (!File.Exists(profilePath))
            return Results.BadRequest("This email is not registered ");

        var json = File.ReadAllText(profilePath);
        var user = JsonSerializer.Deserialize<UserProfile>(json);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Results.BadRequest("Invalid password.");

        if (!user.IsEmailVerified)
            return Results.BadRequest("Please verify your email before logging in.");

        var token = jwt.GenerateToken(user.Email, user.Role, user.Name);

        return Results.Ok(new
        {
            token,
            name = user.Name,
            role = user.Role,
            initials = GetInitials(user.Name)
        });
    }

    private string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p => p[0])).ToUpper();
    }

    public async Task<IResult> RegisterUserAsAdmin(HttpRequest request)
    {
        var form = await request.ReadFormAsync();

        var name = form["name"].ToString().Trim();
        var email = form["email"].ToString().Trim();
        var role = form["role"].ToString().Trim(); // Admin, Author, Editor, Member
        var password = form["password"].ToString();
        var wantsNewsletter = form["newsletter"] == "on";

        // Basic validation
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest("Invalid name.");

        if (string.IsNullOrWhiteSpace(role))
            return Results.BadRequest("Invalid role selected.");

        if (password.Length < 6)
            return Results.BadRequest("Password must be at least 6 characters long.");

        if (UserExists(email))
            return Results.BadRequest("This email is already registered.");

        // Hash password securely
        var hashed = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new UserProfile
        {
            Name = name,
            Email = email,
            PasswordHash = hashed,
            Role = role,
            IsSubscribedToNewsletter = wantsNewsletter,
            IsEmailVerified = true
        };

        SaveUser(user);

        return Results.Ok("User created successfully!");
    }


    public Dictionary<string, int> GetUserRoleCounts()
    {
        var roles = new[] { "Admin", "Author", "Editor", "Member" };
        var result = roles.ToDictionary(role => role + "s", role => 0); // plural keys

        var usersRoot = Path.Combine("content", "users");
        if (!Directory.Exists(usersRoot)) return result;

        foreach (var dir in Directory.GetDirectories(usersRoot))
        {
            var profilePath = Path.Combine(dir, "Profile.json");
            if (!File.Exists(profilePath)) continue;

            var json = File.ReadAllText(profilePath);
            var user = JsonSerializer.Deserialize<UserProfile>(json);
            if (user == null || string.IsNullOrWhiteSpace(user.Role)) continue;

            var key = user.Role + "s"; // pluralize
            if (result.ContainsKey(key)) result[key]++;
        }

        return result;
    }
    public List<UserProfile> GetUsersByRole(string role)
    {
        var result = new List<UserProfile>();
        var usersRoot = Path.Combine("content", "users");

        if (!Directory.Exists(usersRoot)) return result;

        foreach (var dir in Directory.GetDirectories(usersRoot))
        {
            var profilePath = Path.Combine(dir, "Profile.json");
            if (!File.Exists(profilePath)) continue;

            var json = File.ReadAllText(profilePath);
            var user = JsonSerializer.Deserialize<UserProfile>(json);

            if (user != null && user.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                result.Add(user);
        }

        return result;
    }

    public async Task<IResult> ChangeUserRole(HttpRequest request)
    {
        var form = await request.ReadFormAsync();
        var email = form["email"].ToString().Trim();
        var newRole = form["role"].ToString().Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newRole))
            return Results.BadRequest("Missing data.");

        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var userDir = Path.Combine("content", "users", dirName);
        var profilePath = Path.Combine(userDir, "Profile.json");

        if (!File.Exists(profilePath))
            return Results.BadRequest("User not found.");

        var json = await File.ReadAllTextAsync(profilePath);
        var user = JsonSerializer.Deserialize<UserProfile>(json);
        if (user == null) return Results.BadRequest("Corrupted profile.");

        user.Role = newRole;
        var updated = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(profilePath, updated);

        return Results.Ok("Role updated.");
    }

    public async Task<IResult> DeleteUser(HttpRequest request)
    {
        var form = await request.ReadFormAsync();
        var email = form["email"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest("Missing email.");

        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var userDir = Path.Combine("content", "users", dirName);

        if (!Directory.Exists(userDir)) return Results.BadRequest("User not found.");

        Directory.Delete(userDir, true);
        return Results.Ok("User deleted.");
    }


}