namespace DemoDomain.Entities;

public class Entry
{
    public string API { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;
    public bool HTTPS { get; set; }
    public string Cors { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class EntryResponse
{
    public int Count { get; set; }
    public Entry[]? Entries { get; set; }
}