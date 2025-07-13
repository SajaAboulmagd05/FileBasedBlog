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



//refactoring 
//to fix the problem of enum values in json format
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() } // This handles enum conversion
};

//server can serve files inside content/posts/.../assets
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "content")),
    RequestPath = "/content"
});

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


//Api to get a specific post 
app.MapGet("/api/posts/{slug}", ([FromServices] PostQueryService queryService, string slug) =>
{
    var post = queryService.GetPostBySlug(slug);
    return post != null ? Results.Json(post) : Results.NotFound("Post not found.");
});

app.Run();