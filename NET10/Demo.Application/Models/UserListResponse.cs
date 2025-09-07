using System.ComponentModel;
using Demo.Application.DTO.User;

namespace Demo.Application.Models;

/// <summary>
/// 사용자 목록 응답
/// </summary>
public class UserListResponse
{
    /// <summary>
    /// 검색된 정보
    /// </summary>
    public List<UserDto> Items { get; set; } = new();
    /// <summary>
    /// 전체 Row 숫자
    /// </summary>
    [DefaultValue(0)]
    public int TotalItems { get; set; }
}
