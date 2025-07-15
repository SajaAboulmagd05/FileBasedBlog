using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class UserAuthService
{
    private readonly string usersPath = Path.Combine("content", "users");

    public UserProfile? Authenticate(string username, string password)
    {
        var userDir = Directory.GetDirectories(usersPath)
            .FirstOrDefault(dir => dir.EndsWith(username, StringComparison.OrdinalIgnoreCase));

        if (userDir == null) return null;

        var profilePath = Path.Combine(userDir, "Profile.json");
        if (!File.Exists(profilePath)) return null;

        var json = File.ReadAllText(profilePath);
        var user = JsonSerializer.Deserialize<UserProfile>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (user == null) return null;

        var inputHash = ComputeHash(password);
        return inputHash == user.PasswordHash ? user : null;
    }

    private string ComputeHash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
