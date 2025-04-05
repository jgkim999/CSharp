namespace Unity.Tools;

public class ConfigOption
{
    public string AssetPath { get; set; } = string.Empty;
    public string AssetDbPath { get; set; } = string.Empty;
    public string BuildCachePath { get; set; } = string.Empty;
    public string BuildCacheDbPath { get; set; } = string.Empty;
    public string[] IgnoreDirectoryNames { get; set; } = [];
    public string[] FileExtAnalyze { get; set; } = [];
    public string[] IgnoreGuids { get; set; } = [];
}
