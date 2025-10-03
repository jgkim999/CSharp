using Xunit;

namespace Demo.Infra.Tests.Fixtures;

/// <summary>
/// 모든 통합 테스트가 공유하는 컨테이너 컬렉션 정의
/// 이 컬렉션을 사용하면 RabbitMQ와 Valkey 컨테이너가 한 번만 생성됩니다
/// </summary>
[CollectionDefinition("Container Collection")]
public class ContainerCollectionFixture : ICollectionFixture<ContainerFixture>
{
    // 이 클래스는 xUnit이 인식하기 위한 마커 클래스입니다
    // 실제 구현은 필요하지 않습니다
}