namespace Unity.Tools;

public class ConfigOption
{
    public string? BaseDir { get; set; } = string.Empty;
    public string[] IgnoreDirectoryNames { get; set; } = [];
    public string[] FileExtAnalyze { get; set; } = [];
    public string[] IgnoreGuids { get; set; } = [];
}
