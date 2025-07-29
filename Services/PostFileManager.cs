using System.Text.Json;

public static class PostFileManager
{
    public static async Task<List<Post>> GetAllPostsAsync()
    {
        var posts = new List<Post>();
        var postDirectories = Directory.GetDirectories(Path.Combine("content", "posts"));

        foreach (var dir in postDirectories)
        {
            var metaPath = Path.Combine(dir, "meta.json");
            if (!File.Exists(metaPath)) continue;

            var json = await File.ReadAllTextAsync(metaPath);
            var post = JsonSerializer.Deserialize<Post>(json);
            if (post != null) posts.Add(post);
        }
        return posts;
    }

    public static async Task SavePostAsync(Post post)
    {
        var dirName = post.CreatedAt.ToString("yyyy-MM-dd") + "-" + post.Slug;
        var postDir = Path.Combine("content", "posts", dirName);
        var metaPath = Path.Combine(postDir, "meta.json");

        var json = JsonSerializer.Serialize(post, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metaPath, json);
    }
}
