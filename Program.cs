using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CategoryTagService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<PostService>();
var app = builder.Build();
app.UseDefaultFiles(); // Serves index.html by default
app.UseStaticFiles();


// Ensure file-based storage directories exist before running the API
EnsureDirectoriesExist();



// Function to create necessary directories for file-based storage
static void EnsureDirectoriesExist()
{
    string[] directories = {
        "content/posts",
        "content/users",
        "content/categories",
        "content/tags",
        "config"
    };

    foreach (string dir in directories)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}

app.MapGet("/api/tags", () =>
{
    var path = "content/tags/tag-name.json";
    if (!System.IO.File.Exists(path))
        return Results.NotFound("Tag file not found.");

    var json = System.IO.File.ReadAllText(path);
    return Results.Content(json, "application/json");
});

app.MapGet("/api/categories", () =>
{
    var path = "content/categories/category-name.json";
    if (!System.IO.File.Exists(path))
        return Results.NotFound("Tag file not found.");

    var json = System.IO.File.ReadAllText(path);
    return Results.Content(json, "application/json");
});


// //loading the posts for the frontend display 
// app.MapGet("/api/posts", () =>
// {
//     var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
//     if (!Directory.Exists(postsPath))
//         return Results.Json(new { posts = new List<object>() });

//     var allPostFolders = Directory.GetDirectories(postsPath);
//     var publishedPosts = new List<object>();

//     foreach (var folder in allPostFolders)
//     {
//         var metaPath = Path.Combine(folder, "meta.json");
//         if (!File.Exists(metaPath)) continue;

//         var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
//         if (metadata == null || !metadata.ContainsKey("Status")) continue;

//         if (metadata["Status"]?.ToString() != "Published") continue;

//         var slug = metadata.ContainsKey("CustomUrl") ? metadata["CustomUrl"].ToString() : Path.GetFileName(folder);
//         var assetsFolder = Path.Combine(folder, "assets");

//         var imageFiles = new List<string>();
//         var attachmentFiles = new List<string>();

//         if (Directory.Exists(assetsFolder))
//         {
//             var files = Directory.GetFiles(assetsFolder);

//             var supportedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" };

//             imageFiles = files
//               .Where(f => supportedImageExtensions.Contains(Path.GetExtension(f).ToLower()))
//               .ToList();

//             attachmentFiles = files.Except(imageFiles).ToList();
//         }


//         publishedPosts.Add(new
//         {
//             Title = metadata["Title"],
//             Description = metadata["Description"],
//             CreatedAt = metadata["CreatedAt"],
//             Tags = metadata["Tags"],
//             Categories = metadata["Categories"],
//             ReadingTime = metadata.ContainsKey("ReadingTime")
//             ? metadata["ReadingTime"]?.ToString() ?? "1 min read"
//             : "1 min read",
//             Slug = slug,
//             Image = imageFiles.FirstOrDefault() != null
//               ? "/content/posts/" + Path.GetFileName(folder) + "/assets/" + Path.GetFileName(imageFiles.First())
//             : null,

//             AttachmentCount = attachmentFiles.Count
//         });
//     }

//     var sorted = publishedPosts
//         .OrderByDescending(p => DateTime.Parse(p.GetType().GetProperty("CreatedAt")?.GetValue(p)?.ToString() ?? ""))
//         .ToList();

//     return Results.Json(new { posts = sorted });
// });


app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "content")),
    RequestPath = "/content"
});

// api to get posts by category
app.MapGet("/api/posts/by-category", (string category) =>
{
    var categoryFilePath = "content/categories/category-name.json";
    if (!File.Exists(categoryFilePath)) return Results.NotFound("Category file not found.");

    var categoryJson = File.ReadAllText(categoryFilePath);
    var categoryList = JsonSerializer.Deserialize<List<Category>>(categoryJson);
    var selected = categoryList?.FirstOrDefault(c => c.Name.Equals(category, StringComparison.OrdinalIgnoreCase));

    if (selected == null || selected.AssociatedPosts.Count == 0)
        return Results.Json(new { posts = new List<object>() });

    var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
    var posts = new List<object>();

    foreach (var slug in selected.AssociatedPosts)
    {
        var folder = Directory.GetDirectories(postsPath).FirstOrDefault(f => f.Contains(slug));
        if (folder == null) continue;

        var metaPath = Path.Combine(folder, "meta.json");
        if (!File.Exists(metaPath)) continue;

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
        if (metadata == null || metadata["Status"]?.ToString() != "Published") continue;

        var assetsFolder = Path.Combine(folder, "assets");
        var imageFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder)
                .Where(f => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" }
                .Contains(Path.GetExtension(f).ToLower()))
                .ToList()
            : new List<string>();

        var attachmentFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder).Except(imageFiles).ToList()
            : new List<string>();

        posts.Add(new
        {
            Title = metadata["Title"],
            Description = metadata["Description"],
            CreatedAt = metadata["CreatedAt"],
            Tags = metadata["Tags"],
            Categories = metadata["Categories"],
            ReadingTime = metadata.ContainsKey("ReadingTime")
            ? metadata["ReadingTime"]?.ToString() ?? "1 min read"
            : "1 min read",
            Slug = slug,
            Image = imageFiles.FirstOrDefault() != null
              ? "/content/posts/" + Path.GetFileName(folder) + "/assets/" + Path.GetFileName(imageFiles.First())
            : null,

            AttachmentCount = attachmentFiles.Count

        });
    }

    return Results.Json(new { posts });
});

//api to get posts by tags 
app.MapGet("/api/posts/by-tags", (string tags) =>
{
    var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (tagList.Length == 0)
        return Results.Json(new { posts = new List<object>() });

    var tagFilePath = "content/tags/tag-name.json";
    if (!File.Exists(tagFilePath)) return Results.NotFound("Tag file not found.");

    var tagJson = File.ReadAllText(tagFilePath);
    var tagData = JsonSerializer.Deserialize<List<Tag>>(tagJson);

    var matchingSlugs = tagData
        .Where(t => tagList.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
        .SelectMany(t => t.AssociatedPosts)
        .Distinct()
        .ToList();

    var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
    var posts = new List<object>();

    foreach (var slug in matchingSlugs)
    {
        var folder = Directory.GetDirectories(postsPath).FirstOrDefault(f => f.Contains(slug));
        if (folder == null) continue;

        var metaPath = Path.Combine(folder, "meta.json");
        if (!File.Exists(metaPath)) continue;

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
        if (metadata == null || metadata["Status"]?.ToString() != "Published") continue;

        var assetsFolder = Path.Combine(folder, "assets");
        var imageFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder)
                .Where(f => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" }
                .Contains(Path.GetExtension(f).ToLower()))
                .ToList()
            : new List<string>();

        var attachmentFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder).Except(imageFiles).ToList()
            : new List<string>();

        posts.Add(new
        {
            Title = metadata["Title"],
            Description = metadata["Description"],
            CreatedAt = metadata["CreatedAt"],
            Tags = metadata["Tags"],
            Categories = metadata["Categories"],
            ReadingTime = metadata.ContainsKey("ReadingTime")
            ? metadata["ReadingTime"]?.ToString() ?? "1 min read"
            : "1 min read",
            Slug = slug,
            Image = imageFiles.FirstOrDefault() != null
              ? "/content/posts/" + Path.GetFileName(folder) + "/assets/" + Path.GetFileName(imageFiles.First())
            : null,

            AttachmentCount = attachmentFiles.Count
        });
    }

    return Results.Json(new { posts });
});

//search across post title, descritpion, content for the search content
app.MapGet("/api/posts/search", (string query) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.Json(new { posts = new List<object>() });

    var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
    var allPostFolders = Directory.GetDirectories(postsPath);
    var matchingPosts = new List<object>();

    foreach (var folder in allPostFolders)
    {
        var metaPath = Path.Combine(folder, "meta.json");
        if (!File.Exists(metaPath)) continue;



        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
        if (metadata == null || !metadata.ContainsKey("Status"))
        {
            Console.WriteLine($"Skipping post in folder: {folder} — missing 'Status'");
            continue;
        }
        if (metadata == null || metadata["Status"]?.ToString() != "Published") continue;

        var title = metadata["Title"]?.ToString() ?? "";
        var description = metadata["Description"]?.ToString() ?? "";

        // Optional: search in content.md
        var contentPath = Path.Combine(folder, "content.md");
        var body = File.Exists(contentPath) ? File.ReadAllText(contentPath) : "";

        var combined = $"{title} {description} {body}".ToLower();
        if (!combined.Contains(query.ToLower())) continue;

        var assetsFolder = Path.Combine(folder, "assets");
        var imageFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder)
                .Where(f => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" }
                .Contains(Path.GetExtension(f).ToLower()))
                .ToList()
            : new List<string>();

        var attachmentFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder).Except(imageFiles).ToList()
            : new List<string>();

        matchingPosts.Add(new
        {
            Title = metadata["Title"],
            Description = metadata["Description"],
            CreatedAt = metadata["CreatedAt"],
            Tags = metadata["Tags"],
            Categories = metadata["Categories"],
            ReadingTime = metadata.ContainsKey("ReadingTime")
            ? metadata["ReadingTime"]?.ToString() ?? "1 min read"
            : "1 min read",
            Slug = metadata["CustomUrl"],
            Image = imageFiles.FirstOrDefault() != null
              ? "/content/posts/" + Path.GetFileName(folder) + "/assets/" + Path.GetFileName(imageFiles.First())
            : null,

            AttachmentCount = attachmentFiles.Count
        });
    }

    return Results.Json(new { posts = matchingPosts });
});


//Api to get a specific post 
app.MapGet("/api/posts/{slug}", (string slug) =>
{
    var postFolder = Directory.GetDirectories("content/posts")
        .FirstOrDefault(dir => dir.EndsWith(slug));

    if (postFolder == null)
        return Results.NotFound();

    var metaPath = Path.Combine(postFolder, "meta.json");
    var contentPath = Path.Combine(postFolder, "content.md");

    if (!File.Exists(metaPath) || !File.Exists(contentPath))
        return Results.NotFound();

    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
    var content = File.ReadAllText(contentPath);

    if (metadata == null)
    {
        return Results.Problem("Post metadata could not be read.");
    }
    var readingTime = metadata.ContainsKey("ReadingTime")
        ? metadata["ReadingTime"]?.ToString() ?? "1 min read"
        : "1 min read";


    var assetsFolder = Path.Combine(postFolder, "assets");
    var imageFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder)
                .Where(f => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" }
                .Contains(Path.GetExtension(f).ToLower()))
                .ToList()
            : new List<string>();

    var attachmentFiles = Directory.Exists(assetsFolder)
            ? Directory.GetFiles(assetsFolder).Except(imageFiles).ToList()
            : new List<string>();

    return Results.Json(new
    {
        //Id = metadata["Id"],
        Title = metadata["Title"],
        Description = metadata["Description"],
        Slug = metadata["CustomUrl"],
        CreatedAt = metadata["CreatedAt"],
        //UpdatedAt = metadata.ContainsKey("UpdatedAt") ? metadata["UpdatedAt"] : null,
        //ScheduledAt = metadata.ContainsKey("ScheduledAt") ? metadata["ScheduledAt"] : null,
        ReadingTime = readingTime,
        //LikeCount = metadata.ContainsKey("LikeCount") ? Convert.ToInt32(metadata["LikeCount"]) : 0,
        //LikedByUserIds = metadata.ContainsKey("LikedByUserIds") ? ((JsonElement)metadata["LikedByUserIds"]).EnumerateArray().Select(x => x.ToString()).ToList() : new List<string>(),
        //Comments = metadata.ContainsKey("Comments") ? ((JsonElement)metadata["Comments"]).EnumerateArray().Select(c => JsonSerializer.Deserialize<Comment>(c.GetRawText())).ToList() : new List<Comment>(),
        //Status = metadata["Status"],
        Image = imageFiles.FirstOrDefault() != null
              ? "/content/posts/" + Path.GetFileName(postFolder) + "/assets/" + Path.GetFileName(imageFiles.First())
            : null,
        Tags = metadata.ContainsKey("Tags") ? ((JsonElement)metadata["Tags"]).EnumerateArray().Select(t => t.ToString()).ToList() : new List<string>(),
        Categories = metadata.ContainsKey("Categories") ? ((JsonElement)metadata["Categories"]).EnumerateArray().Select(c => c.ToString()).ToList() : new List<string>(),
        Content = content
    });
});


//refactoring 

//to fix the problem of enum values in json format
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() } // This handles enum conversion
};


// Helper method to get post assets
static (string? ImageUrl, int AttachmentCount) GetPostAssets(string assetsFolder)
{
    if (!Directory.Exists(assetsFolder))
        return (null, 0);

    var files = Directory.GetFiles(assetsFolder);
    var supportedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" };

    var imageFiles = files
        .Where(f => supportedImageExtensions.Contains(Path.GetExtension(f).ToLower()))
        .ToList();

    var attachmentFiles = files.Except(imageFiles).ToList();

    var imageUrl = imageFiles.FirstOrDefault() != null
        ? $"/content/posts/{Path.GetFileName(Path.GetDirectoryName(assetsFolder))}/assets/{Path.GetFileName(imageFiles.First())}"
        : null;

    return (imageUrl, attachmentFiles.Count);
}

// API Endpoints
//api to draft the posts
app.MapPost("/api/posts/draft", async (HttpRequest request, PostService postService) =>
{
    var form = await request.ReadFormAsync();
    var result = await postService.PublishPostAsync(form, isDraft: true);
    return result.success ? Results.Ok(result.message) : Results.BadRequest(result.message);

});


//api to publish post 
app.MapPost("/api/posts/publish", async (HttpRequest request, PostService postService) =>
{
    Console.WriteLine("Publish post API hit.");
    var form = await request.ReadFormAsync();
    Console.WriteLine($"Title: {form["title"]}");

    var result = await postService.PublishPostAsync(form);
    return result.success ? Results.Ok(result.message) : Results.BadRequest(result.message);
});


// loading the posts for the frontend display 
app.MapGet("/api/posts", () =>
{
    var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
    if (!Directory.Exists(postsPath))
        return Results.Json(new { posts = new List<object>() });

    var allPostFolders = Directory.GetDirectories(postsPath);
    var publishedPosts = new List<PostSummary>();


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

            var (imageUrl, attachmentCount) = GetPostAssets(assetsFolder);

            publishedPosts.Add(new PostSummary
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

    var sortedPosts = publishedPosts
    .OrderByDescending(p => p.CreatedAt)
    .ToList();


    return Results.Json(new { posts = sortedPosts });
});


app.Run();