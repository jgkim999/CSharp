using FastEndpoints;

namespace Demo.SimpleSocket.Endpoints.Test;

/// <summary>
/// 테스트 관련 엔드포인트들을 그룹화하는 기본 그룹 클래스
/// FastEndpoints에서 API 엔드포인트의 조직화 및 Swagger 문서화를 위해 사용됩니다
/// </summary>
public sealed class TestGroup : Group
{
    /// <summary>
    /// TestGroup의 새 인스턴스를 초기화하고 "admin" 그룹으로 구성합니다
    /// </summary>
    public TestGroup()
    {
        Configure(
            "test",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("Test"));
            });
    }
}
