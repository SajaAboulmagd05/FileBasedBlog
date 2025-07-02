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
            imageFiles = files.Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg")).ToList();
            attachmentFiles = files.Except(imageFiles).ToList();
        }

        publishedPosts.Add(new
        {
            Title = metadata["Title"],
            Description = metadata["Description"],
            CreatedAt = metadata["CreatedAt"],
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

app.Run();