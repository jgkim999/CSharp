using Demo.Application.DTO.User;
using Demo.Domain.Repositories;
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
}