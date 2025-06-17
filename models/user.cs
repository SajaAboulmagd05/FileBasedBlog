public class UserProfile
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Author, Editor
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}