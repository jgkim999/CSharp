using System.ComponentModel;
using Demo.Application.DTO.Company;

namespace Demo.Application.Models;

/// <summary>
/// 회사 목록 응답
/// </summary>
public class CompanyListResponse
{
    /// <summary>
    /// 검색된 정보
    /// </summary>
    public List<CompanyDto> Items { get; set; } = new();
    /// <summary>
    /// 전체 Row 숫자
    /// </summary>
    [DefaultValue(0)]
    public int TotalItems { get; set; }
}