using System.Diagnostics;
using Demo.Domain.Commands;

namespace Demo.Application.Handlers.Commands.Sod;

/// <summary>
/// SOD 명령의 기본 추상 클래스
/// </summary>
public abstract class SodCommand : ICommandJob
{
    /// <summary>
    /// 지정된 클라이언트 IP 주소와 선택적 부모 활동으로 SodCommand 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="clientIp">명령과 연결된 클라이언트의 IP 주소</param>
    /// <param name="parentActivity">추적 또는 진단을 위한 선택적 부모 Activity</param>
    protected SodCommand(string clientIp, Activity? parentActivity)
    {
        ClientIp = clientIp;
        ParentActivity = parentActivity;
    }

    /// <summary>
    /// 클라이언트 IP 주소
    /// </summary>
    public string ClientIp { get; set; }

    /// <summary>
    /// 부모 활동 (추적용)
    /// </summary>
    public Activity? ParentActivity { get; set; }

    /// <summary>
    /// 제공된 서비스 공급자를 사용하여 명령을 비동기적으로 실행하고 취소를 지원합니다
    /// </summary>
    /// <param name="serviceProvider">명령에 필요한 종속성을 해결하는 데 사용되는 서비스 공급자</param>
    /// <param name="ct">명령을 실행하는 동안 관찰할 취소 토큰</param>
    /// <returns>명령의 비동기 실행을 나타내는 작업</returns>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}