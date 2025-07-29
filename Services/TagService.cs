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


    public async Task<string> UpdateTagAsync(string id, string newName)
    {
        var tags = await GetAllTagsAsync();
        var tag = tags.FirstOrDefault(t => t.Id == id);
        if (tag == null) return "Tag not found.";

        var oldName = tag.Name;
        tag.Name = newName;
        await SaveTagsAsync(tags);

        var posts = await PostFileManager.GetAllPostsAsync();
        foreach (var post in posts)
        {
            if (post.Tags.Contains(oldName))
            {
                post.Tags = post.Tags.Select(t => t == oldName ? newName : t).ToList();
                await PostFileManager.SavePostAsync(post);
            }
        }

        return $"Tag '{oldName}' updated to '{newName}'.";
    }

    public async Task<string> DeleteTagAsync(string id)
    {
        var tags = await GetAllTagsAsync();
        var tag = tags.FirstOrDefault(t => t.Id == id);
        if (tag == null) return "Tag not found.";

        tags = tags.Where(t => t.Id != id).ToList();
        await SaveTagsAsync(tags);

        var posts = await PostFileManager.GetAllPostsAsync();
        foreach (var post in posts)
        {
            if (post.Tags.Contains(tag.Name))
            {
                post.Tags.Remove(tag.Name);
                await PostFileManager.SavePostAsync(post);
            }
        }

        return $"Tag '{tag.Name}' deleted and removed from all posts.";
    }

    public async Task SaveTagsAsync(List<Tag> tags)
    {
        var json = JsonSerializer.Serialize(tags, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_tagFilePath, json);
    }

}


