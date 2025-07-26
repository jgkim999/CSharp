using System.Text.Json.Serialization;

namespace GamePulse.DTO;

/// <summary>
/// Response Exam
/// </summary>
[JsonSerializable(typeof(MyResponse))]
public class MyResponse
{
    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// Is age over 18
    /// </summary>
    public bool IsOver18 { get; set; }
}
