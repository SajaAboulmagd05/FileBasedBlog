using System.Text.Json;
using System.Text.RegularExpressions;

public class PostService
{
    // private readonly FileService _fileService;
    private readonly CategoryTagService _categoryTagService;

    public PostService(CategoryTagService categoryTagService)
    {
        _categoryTagService = categoryTagService;
    }

    public async Task<(bool success, string message)> PublishPostAsync(IFormCollection form, string userId, string userName, bool isDraft = false)
    {

        var title = form["title"].ToString().Trim();
        var description = form["description"].ToString().Trim();
        var body = form["body"].ToString().Trim();
        var slugInput = form["slug"].ToString().Trim();


        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(body))
            return (false, "Missing required fields.");

        // Slug handling
        string slug = string.IsNullOrWhiteSpace(slugInput)
            ? GenerateSlug(title)
            : GenerateSlug(slugInput);

        slug = await EnsureUniqueSlugAsync(slug);


        var post = new Post
        {
            Title = title,
            Description = description,
            Slug = slug,
            CreatedAt = DateTime.Now,
            Tags = form["tags"].ToList(),
            Categories = form["categories"].ToList(),
            ReadingTime = EstimateReadingTime(body),
            Author = userName,
            AuthorId = userId
        };

        //  Publishing Options
        if (isDraft)
        {
            post.Status = PostStatus.Draft;
            post.IsPublished = false;
            post.ScheduledAt = null;
        }
        else if (form["publish-option"] == "schedule")
        {
            var date = form["publish-date"].FirstOrDefault();
            var time = form["publish-time"].FirstOrDefault();

            if (DateTime.TryParse($"{date}T{time}", out DateTime scheduledTime))
            {
                post.ScheduledAt = scheduledTime;
                post.Status = PostStatus.Scheduled;
            }
            else return (false, "Invalid publish date/time.");
        }
        else
        {
            post.Status = PostStatus.Published;
            post.IsPublished = true;
        }

        // File-based storage
        var folderPath = Path.Combine("content", "posts", $"{post.CreatedAt:yyyy-MM-dd}-{slug}");
        Directory.CreateDirectory(folderPath);
        Directory.CreateDirectory(Path.Combine(folderPath, "assets"));



        await HandleFileUploads(form, folderPath);

        post.CoverImage = await SaveCoverImageAsync(form.Files["coverImage"], post.Slug, post.CreatedAt);

        Console.WriteLine($"Creating folder for draft at: {folderPath}");

        await File.WriteAllTextAsync(Path.Combine(folderPath, "content.md"), body);

        var metadata = JsonSerializer.Serialize(post);
        await File.WriteAllTextAsync(Path.Combine(folderPath, "meta.json"), metadata);

        // Update tags & categories associated posts 
        _categoryTagService.UpdateCategoriesAndTags(post);


        return (true, "Post published successfully.");
    }
    public string GenerateSlug(string input)
    {
        input = Regex.Replace(input.ToLower(), @"[^a-z0-9\s-]", "");
        input = Regex.Replace(input, @"\s+", "-").Trim('-');
        return input;
    }

    public async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        string finalSlug = baseSlug;
        int counter = 1;

        while (Directory.Exists(Path.Combine("content", "posts", $"{DateTime.Now:yyyy-MM-dd}-{finalSlug}")))
        {
            finalSlug = $"{baseSlug}-{counter++}";
        }

        return finalSlug;
    }

    public async Task<string> SaveCoverImageAsync(IFormFile image, string slug, DateTime createdAt)
    {
        // Step 1: Build your post folder path
        var postFolder = Path.Combine("content", "posts", $"{createdAt:yyyy-MM-dd}-{slug}");
        var assetsFolder = Path.Combine(postFolder, "assets");

        // Ensure folders exist
        Directory.CreateDirectory(postFolder);
        Directory.CreateDirectory(assetsFolder);

        // Save the image as cover.jpg
        var extension = Path.GetExtension(image.FileName);
        var fileName = "cover" + extension; // Always named cover.jpg (or .png)
        var imagePath = Path.Combine(assetsFolder, fileName);

        using var stream = new FileStream(imagePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"assets/{fileName}"; // Relative path used in your Post object
    }

    public string EstimateReadingTime(string content)
    {
        int wordCount = Regex.Matches(content, @"\b\w+\b").Count;
        int minutes = Math.Max(1, wordCount / 200);
        return $"{minutes} min read";
    }

    public async Task HandleFileUploads(IFormCollection form, string postFolder)
    {
        var assetsFolder = Path.Combine(postFolder, "assets");
        Directory.CreateDirectory(assetsFolder); // Ensure it's created

        foreach (var file in form.Files.Where(f => f.Name != "coverImage"))
        {
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(assetsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }
    }

    public async Task<PostStats> GetPostStatsAsync(string userId, string role, bool showAll)
    {
        var allPosts = await LoadAllPostsAsync();
        IEnumerable<Post> relevantPosts = role == "Admin" && showAll
            ? allPosts
            : allPosts.Where(p => p.AuthorId == userId);

        return new PostStats
        {
            DraftPosts = relevantPosts.Count(p => p.Status == PostStatus.Draft),
            ScheduledPosts = relevantPosts.Count(p => p.Status == PostStatus.Scheduled),
            PublishedPosts = relevantPosts.Count(p => p.Status == PostStatus.Published)
        };
    }

    public async Task<List<Post>> GetPostsByStatusAsync(string status, string userId, string role, bool showAll)
    {
        var allPosts = await LoadAllPostsAsync();
        var desiredStatus = Enum.Parse<PostStatus>(status, ignoreCase: true);

        IEnumerable<Post> relevantPosts = role == "Admin" && showAll
            ? allPosts
            : allPosts.Where(p => p.AuthorId == userId);

        return relevantPosts.Where(p => p.Status == desiredStatus).ToList();
    }


    public async Task<List<Post>> LoadAllPostsAsync()
    {
        var posts = new List<Post>();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var postFolders = Directory.GetDirectories("content/posts");

        foreach (var folder in postFolders)
        {
            var metaPath = Path.Combine(folder, "meta.json");

            if (!File.Exists(metaPath)) continue;

            try
            {
                var postJson = await File.ReadAllTextAsync(metaPath);
                var post = JsonSerializer.Deserialize<Post>(postJson, jsonOptions);
                if (post != null) posts.Add(post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing post in '{folder}': {ex.Message}");
            }
        }

        return posts;
    }

    public Task<(bool success, string message)> DeletePostAsync(string slug)
    {
        var postDir = Directory.GetDirectories("content/posts")
            .FirstOrDefault(d => d.EndsWith($"-{slug}"));

        if (postDir == null)
            return Task.FromResult((false, "Post not found."));

        try
        {
            Directory.Delete(postDir, true); // recursive delete
            return Task.FromResult((true, "Post deleted successfully."));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting post: {ex.Message}");
            return Task.FromResult((false, "Failed to delete post."));
        }
    }



    //needs revising 
    public async Task<(bool success, string message)> UpdatePostAsync(IFormCollection form, string userId, string userName)
    {
        var oldSlug = form["oldSlug"].ToString().Trim();
        var matchingDirectory = Directory.GetDirectories("content/posts")
            .FirstOrDefault(d => d.EndsWith($"-{oldSlug}"));

        if (matchingDirectory == null)
            return (false, "Original post not found.");

        var metaPath = Path.Combine(matchingDirectory, "meta.json");
        var contentPath = Path.Combine(matchingDirectory, "content.md");

        var post = JsonSerializer.Deserialize<Post>(await File.ReadAllTextAsync(metaPath));
        if (post == null) return (false, "Unable to deserialize post metadata.");

        // Update fields
        post.Title = form["title"].ToString().Trim();
        post.Description = form["description"].ToString().Trim();
        var body = form["body"].ToString().Trim();

        var slugInput = form["slug"].ToString().Trim();
        post.Slug = string.IsNullOrWhiteSpace(slugInput)
            ? GenerateSlug(post.Title)
            : GenerateSlug(slugInput);

        post.UpdatedAt = DateTime.Now;
        post.Tags = form["tags"].ToList();
        post.Categories = form["categories"].ToList();
        post.ReadingTime = EstimateReadingTime(body);

        // Optional updates: cover image, attached files
        if (form.Files != null && form.Files.Any())
        {
            post.CoverImage = await SaveCoverImageAsync(form.Files["coverImage"], post.Slug, post.CreatedAt);
            await HandleFileUploads(form, matchingDirectory);
        }

        await File.WriteAllTextAsync(contentPath, body);
        var updatedMetadata = JsonSerializer.Serialize(post, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metaPath, updatedMetadata);

        _categoryTagService.UpdateCategoriesAndTags(post);

        return (true, "Post updated successfully.");
    }

}



