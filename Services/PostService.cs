using System.Text.Json;
using System.Text.RegularExpressions;

public class PostService
{
    // private readonly FileService _fileService;
    private readonly CategoryTagService _categoryTagService;

    public PostService(FileService fileService, CategoryTagService categoryTagService)
    {
        // _fileService = fileService;
        _categoryTagService = categoryTagService;
    }

    public async Task<(bool success, string message)> PublishPostAsync(IFormCollection form, bool isDraft = false)
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
        };

        // ⏳ Publishing Options
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

}



