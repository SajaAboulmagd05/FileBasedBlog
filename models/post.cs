
using System.Text.RegularExpressions;
public enum PostStatus
{
    Draft,
    Published,
    Scheduled
}
public class Comment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SubscriberID { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

}
public class Post
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ScheduledAt { get; set; } // Time for future publishing, if there is a publishing time then status is draft
                                               //  changed to published on time of publishing 
    public string ReadingTime { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public int LikeCount { get; set; } = 0;
    public List<string> LikedByUserIds { get; set; } = new List<string>(); // Track who liked the post
    public List<Comment> Comments { get; set; } = new List<Comment>(); // Store comments
    public PostStatus Status { get; set; } = PostStatus.Draft; // Using Enum
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> Categories { get; set; } = new List<string>();
    public bool IsPublished { get; set; } = false;


}
