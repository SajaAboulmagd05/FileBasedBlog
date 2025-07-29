using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("this_is_a_very_secure_jwt_secret_key_1234567890!")
        )
    };
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddSingleton<CategoryTagService>();
builder.Services.AddSingleton<PostService>();
builder.Services.AddSingleton<PostQueryService>();
builder.Services.AddSingleton<PostSchedulerService>();
builder.Services.AddSingleton<SubscriberService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<LikeCommentService>();
builder.Services.AddSingleton<TagService>();
builder.Services.AddSingleton<CategoryService>();
// builder.Services.AddSingleton<PostFileManager>();
var app = builder.Build();

//to change the slug 
// Rewrite pretty URLs (e.g., "/my-awesome-post") to "/post.html?slug=..."
app.MapGet("/{slug:regex(^[a-z0-9-]+$)}", async (HttpContext context, string slug) =>
{
    // Serve post.html with the slug (URL bar stays clean)
    context.Request.QueryString = new QueryString($"?slug={slug}");
    await context.Response.SendFileAsync("wwwroot/post.html");
});

app.UseDefaultFiles(); // Serves index.html by default
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

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



// app.MapPost("/api/subscribe", async (HttpRequest request, SubscriberService subscriberService) =>
// {
//     var form = await request.ReadFormAsync();
//     var email = form["email"].ToString();
//     Console.WriteLine($"Email received: '{email}'");

//     return await subscriberService.HandleSubscription(email);
// });



//api to unsubscribe 
// app.MapGet("/api/unsubscribe", (HttpRequest request, SubscriberService subscriberService) =>
// {
//     var email = request.Query["email"].ToString();

//     if (string.IsNullOrWhiteSpace(email))
//         return Results.BadRequest("Missing email.");

//     var success = subscriberService.Remove(email);
//     return success ? Results.Ok("Unsubscribed.") : Results.NotFound("Subscriber not found.");
// });



app.MapPost("/api/register", async (HttpRequest request, UserService service) =>
    await service.RegisterUser(request));


//add user by admin api
var userGroup = app.MapGroup("/api/users").RequireAuthorization();

userGroup.MapPost("/admin-add", async (HttpRequest request, UserService userService) =>
    await userService.RegisterUserAsAdmin(request));



// retrieve the count of each type of users
app.MapGet("/api/users/counts", () =>
{
    var service = new UserService();
    return Results.Json(service.GetUserRoleCounts());
});

//retrieve users by role 
app.MapGet("/api/users/by-role/{role}", (string role) =>
{
    var service = new UserService();
    var users = service.GetUsersByRole(role);
    return Results.Json(users);
});

//change the role of a user by admin 
app.MapPost("/api/users/change-role", (HttpRequest request) =>
{
    var service = new UserService();
    return service.ChangeUserRole(request);
});


//delete user by admin 
app.MapPost("/api/users/delete", (HttpRequest request) =>
{
    var service = new UserService();
    return service.DeleteUser(request);
});



app.MapGet("/api/verify", (HttpRequest request, UserService service) =>
{
    var email = request.Query["email"].ToString();
    var token = request.Query["token"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        return Results.BadRequest("Missing email or token.");

    var success = service.VerifyUserEmail(email, token);
    if (!success)
        return Results.Redirect("/?token=invalid");
    return Results.Redirect("/?verified=true&showLogin=true");


});

//login api
app.MapPost("/api/login", async (HttpRequest request, UserService userService, JwtService jwt) =>
    await userService.LoginUser(request, jwt));


//api to get post stats for each user for admin display all for authors display their own posts 
app.MapGet("/api/posts/stats", async (HttpContext context, PostService postService) =>
{
    var userId = context.User.FindFirst("UserID")?.Value;
    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
    var queryAll = context.Request.Query["all"] == "true";

    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

    var stats = await postService.GetPostStatsAsync(userId, role, queryAll);
    return Results.Ok(stats);
});



//api to get post by status and for a specific user 
app.MapGet("/api/posts/status", async (HttpContext context, PostService postService) =>
{
    var userId = context.User.FindFirst("UserID")?.Value;
    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
    var status = context.Request.Query["status"].ToString();
    var showAll = context.Request.Query["all"] == "true";

    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

    var posts = await postService.GetPostsByStatusAsync(status, userId, role, showAll);
    return Results.Ok(posts);
});





var postGroup = app.MapGroup("/api/posts").RequireAuthorization();

//api to draft the posts
postGroup.MapPost("/draft", async (HttpContext context, PostService postService) =>
{
    var form = await context.Request.ReadFormAsync();
    var userId = context.User.FindFirst("UserID")?.Value;
    var userName = context.User.FindFirst("name")?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
    {
        return Results.BadRequest("User credentials are missing. Post cannot be created.");
    }

    var result = await postService.PublishPostAsync(form, userId, userName, isDraft: true);
    return result.success ? Results.Ok(result.message) : Results.BadRequest(result.message);
});



//api to publish post 
postGroup.MapPost("/publish", async (HttpContext context, PostService postService) =>
{
    Console.WriteLine("Publish post API hit.");

    var form = await context.Request.ReadFormAsync();
    Console.WriteLine($"Title: {form["title"]}");

    var userId = context.User.FindFirst("UserID")?.Value;
    var userName = context.User.FindFirst("name")?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
    {
        return Results.BadRequest("User credentials are missing. Post cannot be created.");
    }

    var result = await postService.PublishPostAsync(form, userId, userName);
    return result.success ? Results.Ok(result.message) : Results.BadRequest(result.message);
});



postGroup.MapPost("/{slug}/like", async (HttpContext context, string slug, LikeCommentService service) =>
{
    var userId = context.User.FindFirst(ClaimTypes.Email)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
        return Results.Unauthorized();

    return await service.ToggleLike(slug, userId);
});


postGroup.MapPost("/{slug}/comment", async (HttpContext context, HttpRequest req, string slug, LikeCommentService service) =>
{
    var subscriberId = context.User.FindFirst("name")?.Value;
    var form = await req.ReadFormAsync();
    var content = form["content"].ToString();

    if (string.IsNullOrWhiteSpace(subscriberId) || string.IsNullOrWhiteSpace(content))
        return Results.BadRequest("Missing comment data.");

    return await service.AddComment(slug, subscriberId, content);
});

var tagGroup = app.MapGroup("/api/tags").RequireAuthorization();

// Add Tag
tagGroup.MapPost("/add", async (HttpContext context, TagService tagService) =>
{
    var userId = context.User.FindFirst("UserID")?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var form = await context.Request.ReadFormAsync();
    var name = form["name"].ToString();

    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest("Tag name is required.");

    var msg = await tagService.CreateTagAsync(name);
    if (msg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(msg);
    return Results.Ok(msg);
});

// Get All Tags
tagGroup.MapGet("/all", async (TagService tagService) =>
{
    var tags = await tagService.GetAllTagsAsync();
    return Results.Ok(tags);
});

// Update Tag
tagGroup.MapPost("/update", async (HttpContext context, TagService service) =>
{
    var form = await context.Request.ReadFormAsync();
    var id = form["id"].ToString();
    var name = form["name"].ToString();
    var result = await service.UpdateTagAsync(id, name);
    return Results.Ok(result);
});

// Delete Tag
tagGroup.MapPost("/delete", async (HttpContext context, TagService service) =>
{
    var form = await context.Request.ReadFormAsync();
    var id = form["id"].ToString();

    var result = await service.DeleteTagAsync(id);
    return Results.Ok(result);
});


var categoryGroup = app.MapGroup("/api/categories").RequireAuthorization();

// Add Category
categoryGroup.MapPost("/add", async (HttpContext context, CategoryService categoryService) =>
{
    var userId = context.User.FindFirst("UserID")?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var form = await context.Request.ReadFormAsync();
    var name = form["name"].ToString();
    var description = form["description"].ToString();

    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        return Results.BadRequest("Name and description are required.");

    var msg = await categoryService.CreateCategoryAsync(name, description);
    if (msg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(msg);
    return Results.Ok(msg);
});

// Get All Categories
categoryGroup.MapGet("/all", async (CategoryService categoryService) =>
{
    var categories = await categoryService.GetAllCategoriesAsync();
    return Results.Ok(categories);
});

//Update Category
categoryGroup.MapPost("/update", async (HttpContext context, CategoryService service) =>
{
    var form = await context.Request.ReadFormAsync();
    var id = form["id"].ToString();
    var name = form["name"].ToString();
    var description = form["description"].ToString();

    var result = await service.UpdateCategoryAsync(id, name, description);
    return Results.Ok(result);
});

// Delete Category
categoryGroup.MapPost("/delete", async (HttpContext context, CategoryService service) =>
{
    var form = await context.Request.ReadFormAsync();
    var id = form["id"].ToString();

    var result = await service.DeleteCategoryAsync(id);
    return Results.Ok(result);
});


app.Run();