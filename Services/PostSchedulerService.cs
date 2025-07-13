using System.Text.Json;

public class PostSchedulerService
{
    public void ActivateScheduledPosts()
    {
        var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
        var folders = Directory.GetDirectories(postsPath);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var folder in folders)
        {
            var metaPath = Path.Combine(folder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post == null || post.Status != PostStatus.Scheduled || post.ScheduledAt > DateTime.Now) continue;

                // Update post to published
                post.IsPublished = true;
                post.Status = PostStatus.Published;

                var updatedJson = JsonSerializer.Serialize(post);
                File.WriteAllText(metaPath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update scheduled post in {folder}: {ex.Message}");
            }
        }
    }

}
