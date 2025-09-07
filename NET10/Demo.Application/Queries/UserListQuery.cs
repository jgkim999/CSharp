using Demo.Application.DTO.User;
using Demo.Domain.Repositories;
using FluentResults;
using LiteBus.Queries.Abstractions;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Queries;

public record UserListQuery(string? SearchTerm, int Page, int PageSize) : IQuery<Result<UserListQueryResult>>;

public class UserListQueryResult
{
    public List<UserDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
}

public class UserListQueryHandler : IQueryHandler<UserListQuery, Result<UserListQueryResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserListQueryHandler> _logger;

    public UserListQueryHandler(
        IUserRepository userRepository,
        ILogger<UserListQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserListQueryResult>> HandleAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userRepository.GetPagedAsync(query.SearchTerm, query.Page, query.PageSize, cancellationToken);

            if (result.IsFailed)
            {
                _logger.LogError("사용자 목록 조회 실패: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
                return Result.Fail(result.Errors);
            }

            // Domain Entity를 DTO로 변환
            List<UserDto> userDtos = result.Value.Users.Adapt<List<UserDto>>();

            return Result.Ok(new UserListQueryResult
            {
                Items = userDtos,
                TotalItems = result.Value.TotalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 목록 조회 중 예외 발생");
            return Result.Fail($"사용자 목록 조회 실패: {ex.Message}");
        }
    }
}
