//not using it right now 
public class FileService
{
    public async Task SaveAssetsAsync(IFormCollection form, string postFolder)
    {
        var assetsFolder = Path.Combine(postFolder, "assets");
        Directory.CreateDirectory(assetsFolder);

        foreach (var file in form.Files.GetFiles("files"))
        {
            if (file.Length > 0)
            {
                var filePath = Path.Combine(assetsFolder, file.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
        }



        // Optional: handle inline images too
        foreach (var image in form.Files.GetFiles("images"))
        {
            if (image.Length > 0)
            {
                var filePath = Path.Combine(assetsFolder, image.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);
            }
        }
    }

    public async Task<string> SaveCoverImageAsync(IFormFile file, string postFolder)
    {
        var ext = Path.GetExtension(file.FileName);
        var fileName = "cover" + ext;
        var path = Path.Combine(postFolder, fileName);

        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return fileName;
    }

}
