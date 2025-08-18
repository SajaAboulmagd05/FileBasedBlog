public class RoleRequestSummary
{
    public string RequestedRole { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}
public class UserProfile
{

    public string UserID { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Author, Editor, User
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string PasswordHash { get; set; } = string.Empty; // Securely store hashed password
    public bool IsSubscribedToNewsletter { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;
    public RoleRequestSummary? RoleRequest { get; set; }
}