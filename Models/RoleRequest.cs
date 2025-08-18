public enum RequestStatus
{
    Pending,
    Accepted,
    Rejected
}
public class RoleRequest
{
    public string Email { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.Now;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
}
