using System.ComponentModel.DataAnnotations;
using Demo.Domain.Repositories;
using FluentResults;
using LiteBus.Commands.Abstractions;

namespace Demo.Application.Handlers.Commands;

public record ProductCreateCommandResult(Result Result);

public record ProductCreateCommand(long CompanyId, string Name, int Price) : ICommand<ProductCreateCommandResult>;

public class ProductCreateCommandValidator : ICommandValidator<ProductCreateCommand>
{
    /// <summary>
    /// ProductCreateCommand의 유효성을 검사하여 속성들이 유효한지 확인합니다.
    /// </summary>
    /// <param name="command">유효성을 검사할 상품 생성 명령.</param>
    /// <param name="cancellationToken">취소 요청을 모니터링하는 토큰.</param>
    /// <exception cref="ValidationException">속성이 유효하지 않은 경우 발생합니다.</exception>
    public Task ValidateAsync(ProductCreateCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CompanyId <= 0)
            throw new ValidationException("회사 ID는 0보다 커야 합니다");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("상품명은 비어있을 수 없습니다");

        if (command.Price < 0)
            throw new ValidationException("상품 가격은 0 이상이어야 합니다");

        return Task.CompletedTask;
    }
}

public class ProductCreateCommandHandler : ICommandHandler<ProductCreateCommand, ProductCreateCommandResult>
{
    private readonly IProductRepository _repository;

    /// <summary>
    /// 지정된 상품 리포지토리로 ProductCreateCommandHandler 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public ProductCreateCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 리포지토리에서 새 상품을 생성하여 상품 생성 명령을 처리합니다.
    /// </summary>
    /// <param name="command">생성할 상품 세부 정보가 포함된 명령.</param>
    /// <param name="cancellationToken">취소 요청을 모니터링하는 토큰.</param>
    /// <returns>상품 생성 작업의 결과를 나타내는 결과 개체.</returns>
    public async Task<ProductCreateCommandResult> HandleAsync(ProductCreateCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateAsync(command.CompanyId, command.Name, command.Price, cancellationToken);
        return new ProductCreateCommandResult(result);
    }
}