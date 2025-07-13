using System.Text.Json;

public class PostQueryService
{
    public List<PostSummary> GetPublishedPostSummaries()
    {
        var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
        if (!Directory.Exists(postsPath)) return new List<PostSummary>();

        var allPostFolders = Directory.GetDirectories(postsPath);
        var summaries = new List<PostSummary>();
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var folder in allPostFolders)
        {
            var metaPath = Path.Combine(folder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post == null || post.Status != PostStatus.Published) continue;

                var slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug : Path.GetFileName(folder);
                var assetsFolder = Path.Combine(folder, "assets");

                var allFiles = Directory.GetFiles(assetsFolder);
                var attachmentFiles = allFiles
                    .Where(f => !Path.GetFileName(f).ToLower().StartsWith("cover"))
                    .ToList();
                var attachmentCount = attachmentFiles.Count;

                var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
                var imageFile = Directory.GetFiles(assetsFolder)
                    .FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));

                var imageUrl = imageFile != null
                    ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                    : null;

                summaries.Add(new PostSummary
                {
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    Tags = post.Tags ?? new List<string>(),
                    Categories = post.Categories ?? new List<string>(),
                    ReadingTime = post.ReadingTime,
                    Slug = slug,
                    Image = imageUrl,
                    AttachmentCount = attachmentCount,
                    LikeCount = post.LikeCount,
                    CommentCount = post.Comments?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load post from {metaPath}: {ex.Message}");
                continue;
            }
        }

        return summaries.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public List<PostSummary> GetPublishedPostsForCategory(string categoryName)
    {
        var categoryFile = "content/categories/category-name.json";
        if (!File.Exists(categoryFile)) return new List<PostSummary>();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var categories = JsonSerializer.Deserialize<List<Category>>(File.ReadAllText(categoryFile), jsonOptions);
        var summaries = new List<PostSummary>();

        var categoryData = categories?.FirstOrDefault(c =>
            string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

        if (categoryData?.AssociatedPosts == null || !categoryData.AssociatedPosts.Any())
            return summaries;

        foreach (var slug in categoryData.AssociatedPosts)
        {
            var postFolder = Directory.GetDirectories("content/posts")
                .FirstOrDefault(dir => dir.EndsWith(slug));

            if (postFolder == null) continue;

            var metaPath = Path.Combine(postFolder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post?.Status != PostStatus.Published) continue;

                var assetsFolder = Path.Combine(postFolder, "assets");
                var allFiles = Directory.GetFiles(assetsFolder);

                var attachmentCount = allFiles.Count(f =>
                    !Path.GetFileName(f).ToLower().StartsWith("cover"));

                var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
                var imageFile = allFiles.FirstOrDefault(f =>
                    Path.GetFileName(f).ToLower().StartsWith("cover"));

                var imageUrl = imageFile != null
                    ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                    : null;

                summaries.Add(new PostSummary
                {
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    Tags = post.Tags ?? new(),
                    Categories = post.Categories ?? new(),
                    ReadingTime = post.ReadingTime,
                    Slug = post.Slug,
                    Image = imageUrl,
                    AttachmentCount = attachmentCount,
                    LikeCount = post.LikeCount,
                    CommentCount = post.Comments?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load post {slug}: {ex.Message}");
            }
        }

        return summaries.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public List<PostSummary> GetPublishedPostsForTags(IEnumerable<string> tags)
    {
        var tagFile = "content/tags/tag-name.json";
        if (!File.Exists(tagFile)) return new List<PostSummary>();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var allTags = JsonSerializer.Deserialize<List<Tag>>(File.ReadAllText(tagFile), jsonOptions);

        var matchingSlugs = allTags?
            .Where(t => tags.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .SelectMany(t => t.AssociatedPosts)
            .Distinct()
            .ToList();

        if (matchingSlugs == null || matchingSlugs.Count == 0) return new List<PostSummary>();

        var summaries = new List<PostSummary>();

        foreach (var slug in matchingSlugs)
        {
            var postFolder = Directory.GetDirectories("content/posts")
                .FirstOrDefault(dir => dir.EndsWith(slug));

            if (postFolder == null) continue;

            var metaPath = Path.Combine(postFolder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post?.Status != PostStatus.Published) continue;

                var assetsFolder = Path.Combine(postFolder, "assets");
                var allFiles = Directory.Exists(assetsFolder) ? Directory.GetFiles(assetsFolder) : Array.Empty<string>();
                var attachmentCount = allFiles.Count(f => !Path.GetFileName(f).ToLower().StartsWith("cover"));
                var imageFile = allFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));
                var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
                var imageUrl = imageFile != null
                    ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                    : null;

                summaries.Add(new PostSummary
                {
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    Tags = post.Tags ?? new(),
                    Categories = post.Categories ?? new(),
                    ReadingTime = post.ReadingTime,
                    Slug = post.Slug,
                    Image = imageUrl,
                    AttachmentCount = attachmentCount,
                    LikeCount = post.LikeCount,
                    CommentCount = post.Comments?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading post '{slug}': {ex.Message}");
            }
        }

        return summaries.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public List<PostSummary> SearchPublishedPosts(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();

        var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
        if (!Directory.Exists(postsPath)) return new();

        var folders = Directory.GetDirectories(postsPath);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var results = new List<PostSummary>();

        foreach (var folder in folders)
        {
            var metaPath = Path.Combine(folder, "meta.json");
            var contentPath = Path.Combine(folder, "content.md");

            if (!File.Exists(metaPath)) continue;

            var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
            if (post?.Status != PostStatus.Published) continue;

            var content = File.Exists(contentPath) ? File.ReadAllText(contentPath) : string.Empty;

            var combined = $"{post.Title} {post.Description} {content}".ToLower();
            if (!combined.Contains(query.ToLower())) continue;

            var assetsFolder = Path.Combine(folder, "assets");
            var allFiles = Directory.Exists(assetsFolder) ? Directory.GetFiles(assetsFolder) : Array.Empty<string>();
            var attachmentCount = allFiles.Count(f => !Path.GetFileName(f).ToLower().StartsWith("cover"));
            var imageFile = allFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));

            var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
            var imageUrl = imageFile != null
                ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                : null;

            results.Add(new PostSummary
            {
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                Tags = post.Tags ?? new(),
                Categories = post.Categories ?? new(),
                ReadingTime = post.ReadingTime,
                Slug = post.Slug,
                Image = imageUrl,
                AttachmentCount = attachmentCount,
                LikeCount = post.LikeCount,
                CommentCount = post.Comments?.Count ?? 0
            });
        }

        return results.OrderByDescending(p => p.CreatedAt).ToList();
    }

}
