namespace Unity.Tools.Models.Sqlite;

public class BuildCacheFileDirectory
{
    public int DirId { get; set; }
    public string Dirname { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public long Length { get; set; } = 0;
    public string CreationTimeUtc { get; init; }
    public string LastWriteTimeUtc { get; init; }
    public string LastAccessTimeUtc { get; init; }

    public bool IsEqual(BuildCacheFileDirectory rhs)
    {
        if (Dirname != rhs.Dirname ||
            Length != rhs.Length ||
            CreationTimeUtc != rhs.CreationTimeUtc ||
            LastWriteTimeUtc != rhs.LastWriteTimeUtc ||
            LastAccessTimeUtc != rhs.LastAccessTimeUtc)
        {
            return false;
        }
        return true;
    }
}
