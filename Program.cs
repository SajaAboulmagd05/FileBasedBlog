using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CategoryTagService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<PostService>();
builder.Services.AddSingleton<PostQueryService>();
builder.Services.AddSingleton<PostSchedulerService>();
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


app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "content")),
    RequestPath = "/content"
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
app.MapGet("/api/posts", ([FromServices] PostQueryService queryService, [FromServices] PostSchedulerService schedulerService) =>
{
    schedulerService.ActivateScheduledPosts(); // Check & promote scheduled posts
    var posts = queryService.GetPublishedPostSummaries(); // Then return published ones
    return Results.Json(new { posts });
});


// api to get posts by category
app.MapGet("/api/posts/by-category", ([FromServices] PostQueryService queryService, [FromQuery] string category) =>
{
    var posts = queryService.GetPublishedPostsForCategory(category);
    return Results.Json(new { posts });
});

//api to get posts by tags
app.MapGet("/api/posts/by-tags", ([FromServices] PostQueryService queryService, [FromQuery] string tags) =>
{
    var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var posts = queryService.GetPublishedPostsForTags(tagList);
    return Results.Json(new { posts });
});

//search across post title, descritpion, content for the search content
app.MapGet("/api/posts/search", ([FromServices] PostQueryService queryService, [FromQuery] string query) =>
{
    var posts = queryService.SearchPublishedPosts(query);
    return Results.Json(new { posts });
});

app.Run();