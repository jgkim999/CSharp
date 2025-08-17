namespace Demo.Application.DTO;

/// <summary>
/// 응답 예제 DTO
/// </summary>
public class MyResponse
{
    /// <summary>
    /// 전체 이름
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// 18세 이상 여부
    /// </summary>
    public bool IsOver18 { get; set; }
}