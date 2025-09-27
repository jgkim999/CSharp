using FastEndpoints;

namespace Demo.Web.Endpoints.User;

public class UserGroup : Group
{
    /// <summary>
    /// UserGroup의 새 인스턴스를 초기화하고 "admin" 그룹으로 구성합니다
    /// </summary>
    public UserGroup()
    {
        Configure(
            "user",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("user"));
            });
    }
}