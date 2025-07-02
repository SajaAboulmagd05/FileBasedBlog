using System.Text.Json;

public static class JsonFileService
{
    public static T? Load<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }

    public static void Save<T>(T obj, string filePath)
    {
        string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
