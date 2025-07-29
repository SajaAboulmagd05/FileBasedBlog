using System.Text.Json;

public class CategoryService
{
    private readonly string _categoryFilePath = Path.Combine("content", "categories", "category-name.json");

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        if (!File.Exists(_categoryFilePath))
            return new List<Category>();

        var json = await File.ReadAllTextAsync(_categoryFilePath);
        return JsonSerializer.Deserialize<List<Category>>(json) ?? new List<Category>();
    }


    public async Task<string> CreateCategoryAsync(string name, string description)
    {
        var categories = await GetAllCategoriesAsync();
        if (categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Category '{name}' already exists.";
        }
        var newCategory = new Category
        {
            Name = name,
            Description = description
        };

        categories.Add(newCategory);
        var json = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_categoryFilePath, json);

        return $"Category '{name}' created successfully.";

    }

    public async Task<string> UpdateCategoryAsync(string id, string newName, string newDescription)
    {
        var categories = await GetAllCategoriesAsync();
        var category = categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return "Category not found.";

        var oldName = category.Name;

        // Update fields
        category.Name = newName;
        category.Description = newDescription;
        await SaveCategoriesAsync(categories);

        // Update name in all related posts
        var posts = await PostFileManager.GetAllPostsAsync();
        foreach (var post in posts)
        {
            if (post.Categories.Contains(oldName))
            {
                post.Categories = post.Categories.Select(c => c == oldName ? newName : c).ToList();
                await PostFileManager.SavePostAsync(post);
            }
        }

        return $"Category '{oldName}' updated to '{newName}' with description.";
    }

    public async Task<string> DeleteCategoryAsync(string id)
    {
        var categories = await GetAllCategoriesAsync();
        var category = categories.FirstOrDefault(c => c.Id == id);
        if (category == null) return "Category not found.";

        categories = categories.Where(c => c.Id != id).ToList();
        await SaveCategoriesAsync(categories);

        var posts = await PostFileManager.GetAllPostsAsync();
        foreach (var post in posts)
        {
            if (post.Categories.Contains(category.Name))
            {
                post.Categories.Remove(category.Name);
                await PostFileManager.SavePostAsync(post);
            }
        }

        return $"Category '{category.Name}' deleted and removed from all posts.";
    }
    public async Task SaveCategoriesAsync(List<Category> categories)
    {
        var json = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_categoryFilePath, json);
    }

}

