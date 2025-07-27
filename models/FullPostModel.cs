public class FullPostModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ReadingTime { get; set; } = "1 min read";
    public string? Image { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string Content { get; set; } = string.Empty;
    public int AttachmentCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public List<string> Attachments { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();

    public String Author { get; set; } = string.Empty;
}
