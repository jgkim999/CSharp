using System.Diagnostics;

namespace Demo.Application.Commands.Sod;

/// <summary>
/// SOD 명령의 기본 추상 클래스
/// </summary>
public abstract class SodCommand : ICommandJob
{
    /// <summary>
    /// 지정된 클라이언트 IP 주소와 선택적 부모 활동으로 SodCommand 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="clientIp">명령과 연결된 클라이언트의 IP 주소</param>
    /// <summary>
    /// Initializes a new instance of the SodCommand base class with the specified client IP and optional parent Activity for tracing.
    /// </summary>
    /// <param name="clientIp">The IP address of the client associated with this command.</param>
    /// <param name="parentActivity">An optional parent <see cref="Activity"/> used for tracing/diagnostics context.</param>
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
    /// <summary>
/// Asynchronously executes this command.
/// </summary>
/// <remarks>
/// Implementations perform the command's work and may resolve required services from the provided <see cref="IServiceProvider"/>.
/// Implementations must observe the cancellation token and stop promptly when cancellation is requested.
/// </remarks>
/// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
/// <returns>A <see cref="Task"/> representing the asynchronous execution of the command.</returns>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}