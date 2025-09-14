using System.ComponentModel;
using FastEndpoints;
using FluentValidation;

namespace Demo.Application.Models;

public class CompanyCreateRequest
{
    [DefaultValue("삼성전자")]
    public string Name { get; set; } = string.Empty;
}

public class CompanyCreateRequestValidator : Validator<CompanyCreateRequest>
{
    /// <summary>
    /// CompanyCreateRequestValidator 클래스의 새 인스턴스를 초기화하고 회사 생성 요청에 대한 유효성 검사 규칙을 정의합니다.
    /// </summary>
    /// <remarks>
    /// Name 필드가 null이거나 비어있지 않으며, 특정 길이 요구 사항을 충족하도록 강제합니다.
    /// </remarks>
    public CompanyCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("회사명은 필수입니다")
            .NotNull()
            .WithMessage("회사명은 null일 수 없습니다")
            .MinimumLength(2)
            .WithMessage("회사명이 너무 짧습니다")
            .MaximumLength(255)
            .WithMessage("회사명이 너무 깁니다");
    }
}