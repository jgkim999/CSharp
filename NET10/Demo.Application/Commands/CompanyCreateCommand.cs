using System.ComponentModel.DataAnnotations;
using Demo.Domain.Repositories;
using FluentResults;
using LiteBus.Commands.Abstractions;

namespace Demo.Application.Commands;

public record CompanyCreateCommandResult(Result Result);

public record CompanyCreateCommand(string Name) : ICommand<CompanyCreateCommandResult>;

public class CompanyCreateCommandValidator : ICommandValidator<CompanyCreateCommand>
{
    /// <summary>
    /// CompanyCreateCommand의 유효성을 검사하여 Name 속성이 null, 비어있거나 공백이 아닌지 확인합니다.
    /// </summary>
    /// <param name="command">유효성을 검사할 회사 생성 명령.</param>
    /// <param name="cancellationToken">취소 요청을 모니터링하는 토큰.</param>
    /// <exception cref="ValidationException">Name 속성이 유효하지 않은 경우 발생합니다.</exception>
    public Task ValidateAsync(CompanyCreateCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("회사명은 비어있을 수 없습니다");
        return Task.CompletedTask;
    }
}

public class CompanyCreateCommandHandler : ICommandHandler<CompanyCreateCommand, CompanyCreateCommandResult>
{
    private readonly ICompanyRepository _repository;
    
    /// <summary>
    /// 지정된 회사 리포지토리로 CompanyCreateCommandHandler 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public CompanyCreateCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }
    
    /// <summary>
    /// 리포지토리에서 새 회사를 생성하여 회사 생성 명령을 처리합니다.
    /// </summary>
    /// <param name="command">생성할 회사 세부 정보가 포함된 명령.</param>
    /// <param name="cancellationToken">취소 요청을 모니터링하는 토큰.</param>
    /// <returns>회사 생성 작업의 결과를 나타내는 결과 개체.</returns>
    public async Task<CompanyCreateCommandResult> HandleAsync(CompanyCreateCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateAsync(command.Name, cancellationToken);
        return new CompanyCreateCommandResult(result);
    }
}