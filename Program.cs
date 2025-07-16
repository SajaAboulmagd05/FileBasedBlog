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
            Encoding.UTF8.GetBytes("Your_Secret_Key_That_Is_Very_Long")
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
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<PostService>();
builder.Services.AddSingleton<PostQueryService>();
builder.Services.AddSingleton<PostSchedulerService>();
builder.Services.AddSingleton<UserAuthService>();
builder.Services.AddSingleton<SubscriberService>();
builder.Services.AddSingleton<UserService>();
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


//API for user login 
// app.MapPost("/api/auth/login", async (UserLoginRequest request, UserAuthService auth) =>
// {
//     var user = auth.Authenticate(request.Username, request.Password);
//     if (user == null)
//         return Results.Unauthorized();

//     // Build JWT Token
//     var claims = new[]
//     {
//         new Claim(ClaimTypes.Name, user.Username),
//         new Claim(ClaimTypes.Role, user.Role)
//     };

//     var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyHere"));
//     var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//     var token = new JwtSecurityToken(
//         claims: claims,
//         expires: DateTime.UtcNow.AddHours(1),
//         signingCredentials: creds
//     );

//     var jwt = new JwtSecurityTokenHandler().WriteToken(token);

//     return Results.Ok(new { token = jwt, role = user.Role, username = user.Username });
// });

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





app.Run();