using System.Text.Json;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
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


app.MapPost("/api/posts/draft", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    var title = form["title"];
    var description = form["description"];
    var body = form["body"];
    var tags = form["tags"].ToList();
    var categories = form["categories"].ToList();
    var slugValue = form["slug"].ToString();
    //checking if already user provided a slug then use it else create one by replacing the spaces with -
    var slug = string.IsNullOrWhiteSpace(slugValue)
     ? title.ToString().ToLower().Replace(' ', '-')
    : slugValue;

    var createdAt = DateTime.UtcNow;
    var status = "Draft";

    var metadata = new
    {
        Title = title,
        Description = description,
        Tags = tags,
        Categories = categories,
        CustomUrl = slug,
        CreatedAt = createdAt,
        Status = status
    };

    var postFolder = $"content/posts/{createdAt:yyyy-MM-dd}-{slug}";
    Directory.CreateDirectory(postFolder);
    File.WriteAllText(Path.Combine(postFolder, "meta.json"), JsonSerializer.Serialize(metadata));
    File.WriteAllText(Path.Combine(postFolder, "content.md"), body);

    //handling files and image 
    // Create assets folder
    var assetsFolder = Path.Combine(postFolder, "assets");
    Directory.CreateDirectory(assetsFolder);

    // Save uploaded images
    foreach (var imgFile in form.Files.GetFiles("images"))
    {
        if (imgFile.Length > 0)
        {
            var filePath = Path.Combine(assetsFolder, imgFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await imgFile.CopyToAsync(stream);
        }
    }

    // Save attached files
    foreach (var docFile in form.Files.GetFiles("files"))
    {
        if (docFile.Length > 0)
        {
            var filePath = Path.Combine(assetsFolder, docFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await docFile.CopyToAsync(stream);
        }
    }


    return Results.Ok(new { Message = "Draft saved successfully!" });
});

//api to publish the post also checks for publshing options 
app.MapPost("/api/posts/publish", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    var title = form["title"].ToString();
    var description = form["description"].ToString();
    var body = form["body"].ToString();
    var tags = form["tags"].ToList();
    var categories = form["categories"].ToList();
    var slugValue = form["slug"].ToString();
    var slug = string.IsNullOrWhiteSpace(slugValue)
        ? title.ToLower().Replace(' ', '-')
        : slugValue;

    var status = "Published";
    DateTime? publishAt = null;

    if (form["publish-option"] == "schedule")
    {

        // var date = form["publish-date"].ToString();
        // var time = form["publish-time"].ToString();

        var date = form["publish-date"].FirstOrDefault();
        var time = form["publish-time"].FirstOrDefault();

        Console.WriteLine($"date: {date}, time: {time}");

        if (DateTime.TryParse($"{date}T{time}", out DateTime scheduledTime))
        {

            publishAt = scheduledTime;
            status = "Scheduled";
        }
        else
        {
            Console.WriteLine("is the problem in the parsing ");
            return Results.BadRequest(new { Message = "Invalid publish date/time." });
        }

    }

    var metadata = new
    {
        Title = title,
        Description = description,
        Tags = tags,
        Categories = categories,
        CustomUrl = slug,
        CreatedAt = DateTime.UtcNow,
        PublishAt = publishAt,
        Status = status
    };

    var postFolder = $"content/posts/{DateTime.UtcNow:yyyy-MM-dd}-{slug}";
    Directory.CreateDirectory(postFolder);
    File.WriteAllText(Path.Combine(postFolder, "meta.json"), JsonSerializer.Serialize(metadata));
    File.WriteAllText(Path.Combine(postFolder, "content.md"), body);
    Console.WriteLine("Post folder path: " + Path.GetFullPath(postFolder));

    //handling files and image 
    // Create assets folder
    var assetsFolder = Path.Combine(postFolder, "assets");
    Directory.CreateDirectory(assetsFolder);

    // Save uploaded images
    foreach (var imgFile in form.Files.GetFiles("images"))
    {
        if (imgFile.Length > 0)
        {
            var filePath = Path.Combine(assetsFolder, imgFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await imgFile.CopyToAsync(stream);
        }
    }

    // Save attached files
    foreach (var docFile in form.Files.GetFiles("files"))
    {
        if (docFile.Length > 0)
        {
            var filePath = Path.Combine(assetsFolder, docFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await docFile.CopyToAsync(stream);
        }
    }

    if (status == "Published")
    {
        //update categories associated posts 
        var categoryFilePath = "content/categories/category-name.json";
        if (File.Exists(categoryFilePath))
        {
            var categoryJson = File.ReadAllText(categoryFilePath);
            var categoryList = JsonSerializer.Deserialize<List<Category>>(categoryJson) ?? new();

            foreach (var cat in categories)
            {
                var category = categoryList.FirstOrDefault(c => c.Name.Equals(cat, StringComparison.OrdinalIgnoreCase));
                if (category != null && !category.AssociatedPosts.Contains(slug))
                {
                    category.AssociatedPosts.Add(slug);
                }
            }

            File.WriteAllText(categoryFilePath, JsonSerializer.Serialize(categoryList, new JsonSerializerOptions { WriteIndented = true }));
        }

        //update tags associated posts 
        var tagFilePath = "content/tags/tag-name.json";
        if (File.Exists(tagFilePath))
        {
            var tagJson = File.ReadAllText(tagFilePath);
            var tagList = JsonSerializer.Deserialize<List<Tag>>(tagJson) ?? new();

            foreach (var tag in tags)
            {
                var tagEntry = tagList.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (tagEntry != null)
                {
                    if (!tagEntry.AssociatedPosts.Contains(slug))
                        tagEntry.AssociatedPosts.Add(slug);
                }
                else
                {
                    tagList.Add(new Tag
                    {
                        Name = tag,
                        AssociatedPosts = new List<string> { slug }
                    });
                }
            }

            File.WriteAllText(tagFilePath, JsonSerializer.Serialize(tagList, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    return Results.Ok(new { Message = $"{status} post saved successfully!" });
});


//loading the posts for the frontend display 
app.MapGet("/api/posts", () =>
{
    var postsPath = Path.Combine(Directory.GetCurrentDirectory(), "content/posts");
    if (!Directory.Exists(postsPath))
        return Results.Json(new { posts = new List<object>() });

    var allPostFolders = Directory.GetDirectories(postsPath);
    var publishedPosts = new List<object>();

    foreach (var folder in allPostFolders)
    {
        var metaPath = Path.Combine(folder, "meta.json");
        if (!File.Exists(metaPath)) continue;

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(metaPath));
        if (metadata == null || !metadata.ContainsKey("Status")) continue;

        if (metadata["Status"]?.ToString() != "Published") continue;

        var slug = metadata.ContainsKey("CustomUrl") ? metadata["CustomUrl"].ToString() : Path.GetFileName(folder);
        var assetsFolder = Path.Combine(folder, "assets");

        var imageFiles = new List<string>();
        var attachmentFiles = new List<string>();

        if (Directory.Exists(assetsFolder))
        {
            var files = Directory.GetFiles(assetsFolder);

            var supportedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".jfif" };

            imageFiles = files
              .Where(f => supportedImageExtensions.Contains(Path.GetExtension(f).ToLower()))
              .ToList();

            attachmentFiles = files.Except(imageFiles).ToList();
        }


        publishedPosts.Add(new
        {
            Title = metadata["Title"],
            Description = metadata["Description"],
            CreatedAt = metadata["CreatedAt"],
            Tags = metadata["Tags"],
            Categories = metadata["Categories"],
            Slug = slug,
            Image = imageFiles.FirstOrDefault() != null
              ? "/content/posts/" + Path.GetFileName(folder) + "/assets/" + Path.GetFileName(imageFiles.First())
            : null,

            AttachmentCount = attachmentFiles.Count
        });
    }

    var sorted = publishedPosts
        .OrderByDescending(p => DateTime.Parse(p.GetType().GetProperty("CreatedAt")?.GetValue(p)?.ToString() ?? ""))
        .ToList();

    return Results.Json(new { posts = sorted });
});


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
            Slug = metadata["CustomUrl"],
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
            Slug = metadata["CustomUrl"],
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
            Title = title,
            Description = description,
            CreatedAt = metadata["CreatedAt"],
            Slug = metadata["CustomUrl"],
            Image = imageFiles.FirstOrDefault() != null
                ? "/content/posts/" + Path.GetFileName(folder) + "/assets/" + Path.GetFileName(imageFiles.First())
                : null,
            AttachmentCount = attachmentFiles.Count
        });
    }

    return Results.Json(new { posts = matchingPosts });
});

app.Run();