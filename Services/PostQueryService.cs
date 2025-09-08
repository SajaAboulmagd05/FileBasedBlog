using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;


using LuceneDirectory = Lucene.Net.Store.FSDirectory;
using SystemDirectory = System.IO.Directory;

public class PostQueryService
{
    private readonly string _postsPath = Path.Combine(SystemDirectory.GetCurrentDirectory(), "content/posts");
    private readonly string _indexPath = Path.Combine(SystemDirectory.GetCurrentDirectory(), "content/lucene-index");

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public List<PostSummary> GetPublishedPostSummaries()
    {
        var postsPath = Path.Combine(SystemDirectory.GetCurrentDirectory(), "content/posts");
        if (!SystemDirectory.Exists(postsPath)) return new List<PostSummary>();

        var allPostFolders = SystemDirectory.GetDirectories(postsPath);
        var summaries = new List<PostSummary>();
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var folder in allPostFolders)
        {
            var metaPath = Path.Combine(folder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post == null || post.Status != PostStatus.Published) continue;

                var slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug : Path.GetFileName(folder);
                var assetsFolder = Path.Combine(folder, "assets");

                var allFiles = SystemDirectory.GetFiles(assetsFolder);
                var attachmentFiles = allFiles
                    .Where(f => !Path.GetFileName(f).ToLower().StartsWith("cover"))
                    .ToList();
                var attachmentCount = attachmentFiles.Count;

                var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
                var imageFile = SystemDirectory.GetFiles(assetsFolder)
                    .FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));

                var imageUrl = imageFile != null
                    ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                    : null;

                summaries.Add(new PostSummary
                {
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    Tags = post.Tags ?? new List<string>(),
                    Categories = post.Categories ?? new List<string>(),
                    ReadingTime = post.ReadingTime,
                    Slug = slug,
                    Image = imageUrl,
                    AttachmentCount = attachmentCount,
                    LikeCount = post.LikeCount,
                    CommentCount = post.Comments?.Count ?? 0,
                    Author = post.Author
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load post from {metaPath}: {ex.Message}");
                continue;
            }
        }

        return summaries.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public List<PostSummary> GetPublishedPostsForCategory(string categoryName)
    {
        var categoryFile = "content/categories/category-name.json";
        if (!File.Exists(categoryFile)) return new List<PostSummary>();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var categories = JsonSerializer.Deserialize<List<Category>>(File.ReadAllText(categoryFile), jsonOptions);
        var summaries = new List<PostSummary>();

        var categoryData = categories?.FirstOrDefault(c =>
            string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

        if (categoryData?.AssociatedPosts == null || !categoryData.AssociatedPosts.Any())
            return summaries;

        foreach (var slug in categoryData.AssociatedPosts)
        {
            var postFolder = SystemDirectory.GetDirectories("content/posts")
                .FirstOrDefault(dir => dir.EndsWith(slug));

            if (postFolder == null) continue;

            var metaPath = Path.Combine(postFolder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post?.Status != PostStatus.Published) continue;

                var assetsFolder = Path.Combine(postFolder, "assets");
                var allFiles = SystemDirectory.GetFiles(assetsFolder);

                var attachmentCount = allFiles.Count(f =>
                    !Path.GetFileName(f).ToLower().StartsWith("cover"));

                var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
                var imageFile = allFiles.FirstOrDefault(f =>
                    Path.GetFileName(f).ToLower().StartsWith("cover"));

                var imageUrl = imageFile != null
                    ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                    : null;

                summaries.Add(new PostSummary
                {
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    Tags = post.Tags ?? new(),
                    Categories = post.Categories ?? new(),
                    ReadingTime = post.ReadingTime,
                    Slug = post.Slug,
                    Image = imageUrl,
                    AttachmentCount = attachmentCount,
                    LikeCount = post.LikeCount,
                    CommentCount = post.Comments?.Count ?? 0,
                    Author = post.Author
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load post {slug}: {ex.Message}");
            }
        }

        return summaries.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public List<PostSummary> GetPublishedPostsForTags(IEnumerable<string> tags)
    {
        var tagFile = "content/tags/tag-name.json";
        if (!File.Exists(tagFile)) return new List<PostSummary>();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var allTags = JsonSerializer.Deserialize<List<Tag>>(File.ReadAllText(tagFile), jsonOptions);

        var matchingSlugs = allTags?
            .Where(t => tags.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .SelectMany(t => t.AssociatedPosts)
            .Distinct()
            .ToList();

        if (matchingSlugs == null || matchingSlugs.Count == 0) return new List<PostSummary>();

        var summaries = new List<PostSummary>();

        foreach (var slug in matchingSlugs)
        {
            var postFolder = SystemDirectory.GetDirectories("content/posts")
                .FirstOrDefault(dir => dir.EndsWith(slug));

            if (postFolder == null) continue;

            var metaPath = Path.Combine(postFolder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
                if (post?.Status != PostStatus.Published) continue;

                var assetsFolder = Path.Combine(postFolder, "assets");
                var allFiles = SystemDirectory.Exists(assetsFolder) ? SystemDirectory.GetFiles(assetsFolder) : Array.Empty<string>();
                var attachmentCount = allFiles.Count(f => !Path.GetFileName(f).ToLower().StartsWith("cover"));
                var imageFile = allFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));
                var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
                var imageUrl = imageFile != null
                    ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                    : null;

                summaries.Add(new PostSummary
                {
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    Tags = post.Tags ?? new(),
                    Categories = post.Categories ?? new(),
                    ReadingTime = post.ReadingTime,
                    Slug = post.Slug,
                    Image = imageUrl,
                    AttachmentCount = attachmentCount,
                    LikeCount = post.LikeCount,
                    CommentCount = post.Comments?.Count ?? 0,
                    Author = post.Author
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading post '{slug}': {ex.Message}");
            }
        }

        return summaries.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public FullPostModel? GetPostBySlug(string slug)
    {
        var postsPath = Path.Combine("content", "posts");
        var postFolder = SystemDirectory.GetDirectories(postsPath)
            .FirstOrDefault(dir => dir.EndsWith(slug));
        if (postFolder == null) return null;

        var metaPath = Path.Combine(postFolder, "meta.json");
        var contentPath = Path.Combine(postFolder, "content.md");

        if (!File.Exists(metaPath) || !File.Exists(contentPath)) return null;

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), jsonOptions);
        var content = File.ReadAllText(contentPath);
        if (post == null) return null;

        var assetsFolder = Path.Combine(postFolder, "assets");
        var allFiles = SystemDirectory.Exists(assetsFolder) ? SystemDirectory.GetFiles(assetsFolder) : Array.Empty<string>();
        var imageFile = allFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));
        var attachmentCount = allFiles.Count(f => !Path.GetFileName(f).ToLower().StartsWith("cover"));
        var attachmentFiles = allFiles
            .Where(f => !Path.GetFileName(f).ToLower().StartsWith("cover"))
            .Select(f => $"/content/posts/{post.CreatedAt:yyyy-MM-dd}-{post.Slug}/assets/{Path.GetFileName(f)}")
            .ToList();

        var imageUrl = imageFile != null
            ? $"/content/posts/{post.CreatedAt:yyyy-MM-dd}-{post.Slug}/assets/{Path.GetFileName(imageFile)}"
            : null;

        return new FullPostModel
        {
            Title = post.Title,
            Description = post.Description,
            Slug = post.Slug,
            CreatedAt = post.CreatedAt,
            ReadingTime = post.ReadingTime ?? "1 min read",
            Image = imageUrl,
            Tags = post.Tags ?? new(),
            Categories = post.Categories ?? new(),
            Content = content,
            AttachmentCount = attachmentCount,
            LikeCount = post.LikeCount,
            Attachments = attachmentFiles,
            Comments = post.Comments ?? new(),
            Author = post.Author
        };
    }


    public void RebuildLuceneIndex()
    {
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var dir = FSDirectory.Open(_indexPath);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
        using var writer = new IndexWriter(dir, config);

        var folders = SystemDirectory.GetDirectories(_postsPath);

        foreach (var folder in folders)
        {
            var metaPath = Path.Combine(folder, "meta.json");
            var contentPath = Path.Combine(folder, "content.md");

            if (!File.Exists(metaPath)) continue;

            var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), _jsonOptions);
            if (post?.Status != PostStatus.Published) continue;

            var content = File.Exists(contentPath) ? File.ReadAllText(contentPath) : string.Empty;

            var doc = new Document
            {
                new StringField("slug", post.Slug, Field.Store.YES),
                new TextField("title", post.Title ?? "", Field.Store.YES),
                new TextField("description", post.Description ?? "", Field.Store.YES),
                new TextField("content", content, Field.Store.YES),
                new StringField("createdAt", post.CreatedAt.ToString("o"), Field.Store.YES)
            };

            writer.DeleteDocuments(new Term("slug", post.Slug));
            writer.AddDocument(doc);

        }

        writer.Flush(triggerMerge: false, applyAllDeletes: false);
    }

    public List<PostSummary> SearchPublishedPosts(string query)
    {
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var dir = FSDirectory.Open(_indexPath);
        using var reader = DirectoryReader.Open(dir);
        var searcher = new IndexSearcher(reader);

        var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, new[] { "title", "description", "content" }, analyzer)
        {
            DefaultOperator = Operator.AND
        };

        var luceneQuery = parser.Parse(query);


        var hits = searcher.Search(luceneQuery, 20).ScoreDocs;
        var results = new List<PostSummary>();

        foreach (var hit in hits)
        {
            var doc = searcher.Doc(hit.Doc);
            var slug = doc.Get("slug");
            var folder = SystemDirectory.GetDirectories(_postsPath).FirstOrDefault(f => f.EndsWith(slug));
            if (folder == null) continue;

            var metaPath = Path.Combine(folder, "meta.json");
            if (!File.Exists(metaPath)) continue;

            var post = JsonSerializer.Deserialize<Post>(File.ReadAllText(metaPath), _jsonOptions);
            if (post?.Status != PostStatus.Published) continue;

            var assetsFolder = Path.Combine(folder, "assets");
            var allFiles = SystemDirectory.Exists(assetsFolder) ? SystemDirectory.GetFiles(assetsFolder) : Array.Empty<string>();
            var attachmentCount = allFiles.Count(f => !Path.GetFileName(f).ToLower().StartsWith("cover"));
            var imageFile = allFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().StartsWith("cover"));

            var dateSlug = $"{post.CreatedAt:yyyy-MM-dd}-{post.Slug}";
            var imageUrl = imageFile != null
                ? $"/content/posts/{dateSlug}/assets/{Path.GetFileName(imageFile)}"
                : null;

            results.Add(new PostSummary
            {
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                Tags = post.Tags ?? new(),
                Categories = post.Categories ?? new(),
                ReadingTime = post.ReadingTime,
                Slug = post.Slug,
                Image = imageUrl,
                AttachmentCount = attachmentCount,
                LikeCount = post.LikeCount,
                CommentCount = post.Comments?.Count ?? 0,
                Author = post.Author
            });
        }

        return results.OrderByDescending(p => p.CreatedAt).ToList();
    }


}
