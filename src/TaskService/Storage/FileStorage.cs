namespace NullBattle.TaskBank.Storage;

public class FileStorage
{
    private readonly string _folder;

    public FileStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var path = configuration["Storage:TaskFilesPath"] ?? "TaskFiles";
        _folder = Path.GetFullPath(path, environment.ContentRootPath);
        Directory.CreateDirectory(_folder);
    }

    public string GetPath(long taskId)
    {
        return Path.Combine(_folder, taskId.ToString());
    }

    public bool Exists(long taskId)
    {
        return File.Exists(GetPath(taskId));
    }

    public async Task SaveAsync(long taskId, IFormFile file)
    {
        using var stream = new FileStream(GetPath(taskId), FileMode.Create);
        await file.CopyToAsync(stream);
    }
}