using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// 테스트 관련 엔드포인트들을 그룹화하는 기본 그룹 클래스
/// FastEndpoints에서 API 엔드포인트의 조직화 및 Swagger 문서화를 위해 사용됩니다
/// </summary>
public class TestGroup : Group
{
    /// <summary>
    /// TestGroup의 새 인스턴스를 초기화하고 "admin" 그룹으로 구성합니다
    /// </summary>
    public TestGroup()
    {
        Configure(
            "",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("Test"));
            });
    }
}

/// <summary>
/// 메시지 큐(Message Queue) 테스트 관련 엔드포인트들을 그룹화하는 서브 그룹 클래스
/// TestGroup을 상속받아 RabbitMQ 테스트 API들을 조직화합니다
/// </summary>
public class MqTest : SubGroup<TestGroup>
{
    /// <summary>
    /// MqTest의 새 인스턴스를 초기화하고 "Mq Test" 서브그룹으로 구성합니다
    /// </summary>
    public MqTest()
    {
        Configure(
            "",
            ep =>
            {
                ep.Description(
                    x => x.Produces(402)
                        .WithTags("MQ Test"));
            });
    }
}

/// <summary>
/// 로깅 테스트 관련 엔드포인트들을 그룹화하는 서브 그룹 클래스
/// TestGroup을 상속받아 로깅 및 텔레메트리 테스트 API들을 조직화합니다
/// </summary>
public class LoggingTest : SubGroup<TestGroup>
{
    /// <summary>
    /// LoggingTest의 새 인스턴스를 초기화하고 "Logging Test" 서브그룹으로 구성합니다
    /// </summary>
    public LoggingTest()
    {
        Configure(
            "",
            ep =>
            {
                ep.Description(
                    x => x.Produces(403)
                        .WithTags("Logging"));
            });
    }
}
