using System.Text.Json;
using System.Text.RegularExpressions;
using BCrypt.Net;
using System.Text;

public class ProfileService
{
    private readonly string userRoot = Path.Combine("content", "users");
    private readonly string roleRequestRoot = Path.Combine("content", "role-requests");

    public UserProfile? LoadUser(string email)
    {
        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var profilePath = Path.Combine(userRoot, dirName, "Profile.json");

        if (!File.Exists(profilePath)) return null;

        var json = File.ReadAllText(profilePath);
        return JsonSerializer.Deserialize<UserProfile>(json);
    }

    public void SaveUser(UserProfile user)
    {
        var dirName = Regex.Replace(user.Email.ToLower(), @"[@\.]", "_");
        var userDir = Path.Combine(userRoot, dirName);
        Directory.CreateDirectory(userDir);

        var profilePath = Path.Combine(userDir, "Profile.json");
        var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(profilePath, json);
    }

    public IResult ChangeUserName(string email, string newName, JwtService jwt)
    {
        var user = LoadUser(email);
        if (user == null)
            return Results.BadRequest(new { error = "User not found." });

        user.Name = newName;
        SaveUser(user);

        var newToken = jwt.GenerateToken(user.UserID, user.Email, user.Role, user.Name);

        return Results.Ok(new
        {
            message = "Name updated successfully.",
            token = newToken,
            name = user.Name
        });
    }

    public IResult ChangePassword(string email, string currentPassword, string newPassword)
    {
        var user = LoadUser(email);
        if (user == null)
            return Results.BadRequest(new { error = "User not found." });

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return Results.BadRequest(new { error = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        SaveUser(user);

        return Results.Ok(new { message = "Password changed successfully." });
    }

    public IResult DeleteAccount(string email, string password)
    {
        var user = LoadUser(email);
        if (user == null)
            return Results.BadRequest(new { error = "User not found." });

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Results.BadRequest(new { error = "Password is incorrect." });

        var dirName = Regex.Replace(email.ToLower(), @"[@\.]", "_");
        var userDir = Path.Combine(userRoot, dirName);
        if (Directory.Exists(userDir)) Directory.Delete(userDir, true);

        return Results.Ok(new { message = "Account deleted successfully." });
    }

    public IResult SaveRoleRequest(string email, string requestedRole, string message)
    {
        Directory.CreateDirectory(roleRequestRoot);

        var fileName = Regex.Replace(email.ToLower(), @"[@\.]", "_") + ".json";
        var path = Path.Combine(roleRequestRoot, fileName);

        // Check if a request already exists
        if (File.Exists(path))
        {
            try
            {
                var existingJson = File.ReadAllText(path);
                var existingRequest = JsonSerializer.Deserialize<RoleRequest>(existingJson);

                if (existingRequest != null && existingRequest.Status == RequestStatus.Pending)
                {
                    return Results.BadRequest(new { message = "A pending role request already exists. Please wait for it to be reviewed." });
                }
            }
            catch
            {
                return Results.BadRequest(new { message = "Failed to read existing request." });
            }
        }

        // Create new request
        var request = new RoleRequest
        {
            Email = email,
            RequestedRole = requestedRole,
            Message = message
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        return Results.Ok(new { message = "Role change request submitted." });
    }



}
