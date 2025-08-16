using System.Diagnostics;
using Demo.Application.Services;
using Demo.Infra.Services;
using Demo.Application.Commands.Sod;

namespace GamePulse.Sod.Endpoints.Rtt;

public class RttCommand : SodCommand
{
    private readonly int _rtt;
    private readonly int _quality;

    /// <summary>
    /// Initializes a new instance of the <see cref="RttCommand"/> class with the specified client IP address and optional parent activity.
    /// </summary>
    /// <param name="clientIp">The IP address of the client initiating the command.</param>
    /// <param name="quality"></param>
    /// <param name="parentActivity">An optional parent <see cref="Activity"/> for tracing context.</param>
    /// <param name="rtt"></param>
    public RttCommand(string clientIp, int rtt, int quality, Activity? parentActivity)
        : base(clientIp, parentActivity)
    {
        ClientIp = clientIp;
        _rtt = rtt;
        _quality = quality;
    }

    /// <summary>
    /// RTT 명령을 비동기적으로 실행하여 클라이언트 IP를 로깅하고 Activity 스팬을 시작합니다.
    /// </summary>
    /// <param name="serviceProvider">의존성을 해결하는 데 사용되는 서비스 프로바이더</param>
    /// <param name="ct">비동기 작업을 위한 취소 토큰</param>
    /// <returns>비동기 작업을 나타내는 Task</returns>
    public override async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var logger = serviceProvider.GetService<ILogger<RttCommand>>();
        var ipToNationService = serviceProvider.GetService<IIpToNationService>();
        var telemetryService = serviceProvider.GetService<ITelemetryService>();

        // OpenTelemetry Activity 시작 - 부모 Activity와 연결
        using var activity = telemetryService?.StartActivity("rtt.process_command", new Dictionary<string, object?>
        {
            ["client.ip"] = ClientIp,
            ["rtt.value_ms"] = _rtt,
            ["rtt.quality"] = _quality,
            ["command.type"] = "RttCommand"
        });

        try
        {
            telemetryService?.LogInformationWithTrace(logger!,
                "RTT 명령 처리 시작: ClientIP={ClientIP}, RTT={RTT}ms, Quality={Quality}",
                ClientIp, _rtt, _quality);

            // IP 주소를 국가 코드로 변환
            if (ipToNationService == null)
            {
                const string defaultCountryCode = "Unknown";

                telemetryService?.LogWarningWithTrace(logger!,
                    "IpToNationService가 null입니다. 기본 국가 코드 '{CountryCode}'를 사용합니다.", defaultCountryCode);

                // 기본 국가 코드로 메트릭 기록
                var defaultRtt = _rtt / 1000.0;
                telemetryService?.RecordRttMetrics(defaultCountryCode, defaultRtt, _quality, "sod");

                // Activity에 정보 추가
                activity?.SetTag("country.code", defaultCountryCode);
                activity?.SetTag("rtt.value_seconds", defaultRtt);
                activity?.SetTag("service.missing", "IpToNationService");

                telemetryService?.SetActivitySuccess(activity, "RTT processed with default country code");

                telemetryService?.LogInformationWithTrace(logger!,
                    "RTT 처리 완료 (기본값): Game={Game}, ClientIP={ClientIP}, CountryCode={CountryCode}, RTT={RTT}s, Quality={Quality}",
                    "sod", ClientIp, defaultCountryCode, defaultRtt, _quality);

                return;
            }

            // IP 주소에서 국가 코드 조회
            using var geoActivity = telemetryService?.StartActivity("rtt.get_country_code", new Dictionary<string, object?>
            {
                ["client.ip"] = ClientIp
            });

            var countryCode = await ipToNationService.GetNationCodeAsync(ClientIp, ct);

            geoActivity?.SetTag("country.code", countryCode);
            telemetryService?.SetActivitySuccess(geoActivity, "Country code resolved successfully");

            // RTT 처리 - 밀리초를 초 단위로 변환
            var rttSeconds = _rtt / 1000.0;

            // RTT 메트릭 기록
            telemetryService?.RecordRttMetrics(countryCode, rttSeconds, _quality, "sod");

            // 비즈니스 메트릭 기록
            telemetryService?.RecordBusinessMetric("rtt_processed", 1, new Dictionary<string, object?>
            {
                ["country.code"] = countryCode,
                ["game.type"] = "sod"
            });

            // Activity에 최종 정보 추가
            activity?.SetTag("country.code", countryCode);
            activity?.SetTag("rtt.value_seconds", rttSeconds);
            activity?.SetTag("processing.success", true);

            telemetryService?.SetActivitySuccess(activity, "RTT command processed successfully");

            telemetryService?.LogInformationWithTrace(logger!,
                "RTT 처리 완료: Game={Game}, ClientIP={ClientIP}, CountryCode={CountryCode}, RTT={RTT}s, Quality={Quality}",
                "sod", ClientIp, countryCode, rttSeconds, _quality);
        }
        catch (Exception ex)
        {
            // 예외 처리 및 메트릭 기록
            telemetryService?.RecordError("rtt_command_exception", "rtt.process_command", ex.Message);
            telemetryService?.SetActivityError(activity, ex);

            telemetryService?.LogErrorWithTrace(logger!, ex,
                "RTT 명령 처리 중 예외 발생: ClientIP={ClientIP}, RTT={RTT}ms", ClientIp, _rtt);

            throw; // 예외를 다시 던져서 상위 레벨에서 처리하도록 함
        }
    }
}
