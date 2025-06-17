var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles(); // Serves index.html by default
app.UseStaticFiles();


// Ensure file-based storage directories exist before running the API
EnsureDirectoriesExist();

app.Run();

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

