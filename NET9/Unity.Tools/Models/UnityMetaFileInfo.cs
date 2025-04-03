namespace Unity.Tools.Models;

public class UnityMetaFileInfo
{
    public int DirNum { get; set; }
    public string MetaFilename { get; set; } = string.Empty;
    public string Guid { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();

    public string Filename
    {
        get
        {
            if (string.IsNullOrEmpty(MetaFilename))
                return "";
            return MetaFilename.Replace(".meta", "");
        }
    }

    public string Extension => Path.GetExtension(Filename);

    public int DependencyCount => Dependencies.Count;
}
