using System.Text.Json;

public class LikeCommentService
{
    private readonly string postsPath = Path.Combine("content", "posts");

    public async Task<IResult> ToggleLike(string slug, string userId)
    {
        var postsPath = Path.Combine("content", "posts");
        var postFolder = Directory.GetDirectories(postsPath)
            .FirstOrDefault(dir => dir.EndsWith(slug));
        if (postFolder == null) return Results.NotFound();

        var metaPath = Path.Combine(postFolder, "meta.json");


        var json = await File.ReadAllTextAsync(metaPath);
        var post = JsonSerializer.Deserialize<Post>(json);
        if (post == null) return Results.BadRequest("Invalid post format.");

        if (post.LikedByUserIds.Contains(userId))
            post.LikedByUserIds.Remove(userId);
        else
            post.LikedByUserIds.Add(userId);

        post.LikeCount = post.LikedByUserIds.Count;

        var updatedJson = JsonSerializer.Serialize(post, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metaPath, updatedJson);

        return Results.Ok(new { liked = post.LikedByUserIds.Contains(userId), likes = post.LikeCount });
    }

    public async Task<IResult> AddComment(string slug, string subscriberId, string content)
    {
        var postsPath = Path.Combine("content", "posts");
        var postFolder = Directory.GetDirectories(postsPath)
            .FirstOrDefault(dir => dir.EndsWith(slug));
        if (postFolder == null) return Results.NotFound();

        var metaPath = Path.Combine(postFolder, "meta.json");


        var json = await File.ReadAllTextAsync(metaPath);
        var post = JsonSerializer.Deserialize<Post>(json);
        if (post == null) return Results.BadRequest("Invalid post format.");

        post.Comments.Add(new Comment
        {
            SubscriberID = subscriberId,
            Content = content,
            CreatedAt = DateTime.Now
        });

        var updatedJson = JsonSerializer.Serialize(post, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metaPath, updatedJson);

        return Results.Ok(post.Comments);
    }
}
