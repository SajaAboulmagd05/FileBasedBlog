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
        var dirName = Regex.Replace(email.ToLower(), @"[@\\.]", "_");
        var profilePath = Path.Combine(userRoot, dirName, "Profile.json");

        if (!File.Exists(profilePath)) return null;

        var json = File.ReadAllText(profilePath);
        return JsonSerializer.Deserialize<UserProfile>(json);
    }

    public void SaveUser(UserProfile user)
    {
        var dirName = Regex.Replace(user.Email.ToLower(), @"[@\\.]", "_");
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
            return Results.Json(new { error = "User not found." }, statusCode: 400);

        user.Name = newName;
        SaveUser(user);

        var newToken = jwt.GenerateToken(user.UserID, user.Email, user.Role, user.Name);

        return Results.Json(new
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
            return Results.Json(new { error = "User not found." }, statusCode: 400);

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return Results.Json(new { error = "Current password is incorrect." }, statusCode: 400);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        SaveUser(user);

        return Results.Json(new { message = "Password changed successfully." });
    }

    public IResult DeleteAccount(string email, string password)
    {
        var user = LoadUser(email);
        if (user == null)
            return Results.Json(new { error = "User not found." }, statusCode: 400);

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Results.Json(new { error = "Password is incorrect." }, statusCode: 400);

        var dirName = Regex.Replace(email.ToLower(), @"[@\\.]", "_");
        var userDir = Path.Combine(userRoot, dirName);
        if (Directory.Exists(userDir)) Directory.Delete(userDir, true);

        return Results.Json(new { message = "Account deleted successfully." });
    }

    public IResult SaveRoleRequest(string email, string requestedRole, string message)
    {
        var dirName = Regex.Replace(email.ToLower(), @"[@\\.]", "_");
        var profilePath = Path.Combine("content", "users", dirName, "Profile.json");
        var requestPath = Path.Combine(roleRequestRoot, dirName + ".json");

        // Check for existing request
        if (File.Exists(requestPath))
        {
            var existingJson = File.ReadAllText(requestPath);
            var existingRequest = JsonSerializer.Deserialize<RoleRequest>(existingJson);

            if (existingRequest != null && existingRequest.Status == RequestStatus.Pending)
                return Results.Json(new { error = "A pending role request already exists." }, statusCode: 400);
        }

        // Create new request
        var request = new RoleRequest
        {
            Email = email,
            RequestedRole = requestedRole,
            Message = message,
            RequestedAt = DateTime.UtcNow,
            Status = RequestStatus.Pending
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(requestPath, json);

        // Update Profile.json
        if (File.Exists(profilePath))
        {
            try
            {
                var profileJson = File.ReadAllText(profilePath);
                var user = JsonSerializer.Deserialize<UserProfile>(profileJson);

                if (user != null)
                {
                    user.RoleRequest = new RoleRequestSummary
                    {
                        RequestedRole = requestedRole,
                        Status = request.Status.ToString(),
                        Message = message,
                        RequestedAt = request.RequestedAt
                    };

                    var updatedProfile = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(profilePath, updatedProfile);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating profile: {ex.Message}");
                // Optionally log but don't fail the request
            }
        }

        return Results.Json(new { message = "Role change request submitted." });
    }
}
