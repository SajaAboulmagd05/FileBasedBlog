using System.Text.Json;

public class TagService
{
    private readonly string _tagFilePath = Path.Combine("content", "tags", "tag-name.json");

    public async Task<List<Tag>> GetAllTagsAsync()
    {
        if (!File.Exists(_tagFilePath))
            return new List<Tag>();

        var json = await File.ReadAllTextAsync(_tagFilePath);
        return JsonSerializer.Deserialize<List<Tag>>(json) ?? new List<Tag>();
    }

    public async Task<string> CreateTagAsync(string name)
    {
        var tags = await GetAllTagsAsync();

        // Check for duplicates
        if (tags.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Tag '{name}' already exists.";
        }

        var newTag = new Tag
        {
            Name = name
        };

        tags.Add(newTag);
        var json = JsonSerializer.Serialize(tags, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_tagFilePath, json);

        return $"Tag '{name}' created successfully.";
    }

}


