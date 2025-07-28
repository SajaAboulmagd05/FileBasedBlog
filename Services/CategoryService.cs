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
}

