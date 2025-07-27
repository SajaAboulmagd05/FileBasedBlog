public class PostSummary
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> Categories { get; set; } = new List<string>();
    public string ReadingTime { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int AttachmentCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public string Author { get; set; } = string.Empty;
}
