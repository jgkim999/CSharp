using System.ComponentModel;
using FastEndpoints;
using FluentValidation;

namespace Demo.Application.Models;

/// <summary>
/// 회사 정보를 가져온다.
/// </summary>
public class CompanyListRequest
{
    /// <summary>
    /// 검색어
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// 원하는 페이지, 0부터 시작
    /// </summary>
    [DefaultValue(0)]
    public int Page { get; set; } = 0;
    
    /// <summary>
    /// 페이지 크기, 기본 10
    /// </summary>
    [DefaultValue(10)]
    public int PageSize { get; set; } = 10;
}

public class CompanyListRequestValidator : Validator<CompanyListRequest>
{
    public CompanyListRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page must be greater than or equal to 0");
            
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must be between 1 and 100");
    }
}