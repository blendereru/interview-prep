namespace Delegates_Concepts;

public abstract class SystemFile
{
    public abstract string Extension { get; }
    public string Name { get; set; } = null!;
    public string? Content { get; set; }
    public int ContentSize => Content?.Length ?? 0;
}