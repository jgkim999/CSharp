using Demo.Application.DTO.User;
using Demo.Application.Handlers.Queries;
using Demo.Application.Services;
using Demo.Domain.Repositories;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Queries.Abstractions;
using MapsterMapper;

namespace Demo.Web.GraphQL.QueryTypes;

public class UserQueries
{
    /// <summary>
    /// GetUserByIdAsync 리졸버 정의
    /// </summary>
    /// <param name="id"></param>
    /// <param name="userRepository"></param>
    /// <param name="mapper"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /*
    query {
        userById(id: 1) {
            id
            name
            email
            createdAt
        }
    }
     */
    public async Task<UserDto?> GetUserByIdAsync(
        long id,
        IUserRepository userRepository,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        // 주입받은 서비스를 사용하여 비즈니스 로직 호출
        var result = await userRepository.FindByIdAsync(id, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return mapper.Map<UserDto>(result.Value);
        }
        return null;
    }

    /// <summary>
    /// GetUserListAsync 리졸버 정의
    /// 사용자 목록을 조회하며, 페이지네이션과 검색 기능을 지원합니다
    /// </summary>
    /// <param name="searchTerm">검색어</param>
    /// <param name="page">원하는 페이지 (0부터 시작)</param>
    /// <param name="pageSize">페이지 크기 (기본 10)</param>
    /// <param name="queryMediator">CQRS 쿼리 처리를 위한 IQueryMediator</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService</param>
    /// <param name="logger">로깅을 위한 ILogger</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>사용자 목록과 전체 개수를 포함한 페이로드</returns>
    /*
    query {
        userList(searchTerm: "홍길동", page: 0, pageSize: 10) {
            items {
                id
                name
                email
                createdAt
            }
            totalItems
            errors
        }
    }
     */
    public async Task<UserListPayload> GetUserListAsync(
        string? searchTerm,
        int page,
        int pageSize,
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<UserQueries> logger,
        CancellationToken cancellationToken)
    {
        using var activity = telemetryService.StartActivity("user.queries.get.user.list");

        try
        {
            var query = new UserListQuery(searchTerm, page, pageSize);
            var result = await queryMediator.QueryAsync(query, cancellationToken);

            if (result.IsFailed)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                logger.LogError("사용자 목록 조회 실패: {ErrorMessage}", errorMessage);
                var errors = result.Errors.Select(e => e.Message).ToList();
                return new UserListPayload(null, 0, errors);
            }

            return new UserListPayload(result.Value.Items, result.Value.TotalItems);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "사용자 목록 조회 중 예외 발생");
            telemetryService.SetActivityError(activity, ex);
            return new UserListPayload(null, 0, new List<string> { ex.Message });
        }
    }
}