using System.Text.Json;

public class CategoryTagService
{
    public void UpdateCategoriesAndTags(Post post)
    {
        // Update categories
        var categoryPath = "content/categories/category-name.json";
        if (File.Exists(categoryPath))
        {
            var json = File.ReadAllText(categoryPath);
            var list = JsonSerializer.Deserialize<List<Category>>(json) ?? new();

            foreach (var cat in post.Categories)
            {
                var entry = list.FirstOrDefault(c => c.Name.Equals(cat, StringComparison.OrdinalIgnoreCase));
                if (entry != null && !entry.AssociatedPosts.Contains(post.Slug))
                    entry.AssociatedPosts.Add(post.Slug);
            }

            File.WriteAllText(categoryPath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Update tags
        var tagPath = "content/tags/tag-name.json";
        if (File.Exists(tagPath))
        {
            var json = File.ReadAllText(tagPath);
            var list = JsonSerializer.Deserialize<List<Tag>>(json) ?? new();

            foreach (var tag in post.Tags)
            {
                var entry = list.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (entry != null && !entry.AssociatedPosts.Contains(post.Slug))
                    entry.AssociatedPosts.Add(post.Slug);
                else
                    list.Add(new Tag { Name = tag, AssociatedPosts = new List<string> { post.Slug } });
            }

            File.WriteAllText(tagPath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
