namespace GamePulse.DTO;

/// <summary>
/// 요청 예제
/// </summary>
public class MyRequest
{
    /// <summary>
    /// 이름 (성)
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>
    /// 이름 (명)
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>
    /// 나이
    /// </summary>
    public int Age { get; set; }
}

