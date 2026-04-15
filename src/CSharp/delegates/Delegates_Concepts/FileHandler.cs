namespace Delegates_Concepts;

public class FileHandler
{
    private readonly List<SystemFile> _files;
    public FileHandler()
    {
        _files = new List<SystemFile>();
    }

    public void Upload(List<SystemFile> files)
    {
        Thread.Sleep(1000);
        _files.AddRange(files);
        Console.WriteLine("Files were uploaded. Total: {0}", files.Count);
    }
    
    public void DisplayFiles(Predicate<SystemFile>? predicate = null)
    {
        if (predicate != null && _files.Any())
        {
            foreach (var file in _files)
            {
                if (predicate(file))
                {
                    Console.WriteLine($"{file.Name}.{file.Extension}");
                }
            }
        }
    }
}