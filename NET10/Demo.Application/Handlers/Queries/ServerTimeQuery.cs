using System.Diagnostics;
using System.Globalization;
using Demo.Application.Services;
using LiteBus.Queries.Abstractions;
using NodaTime;

namespace Demo.Application.Handlers.Queries;

public record ServerTimeQuery : IQuery<(string utc, string korea, string koreanCalendar)>;

public class ServerTimeQueryHandler : IQueryHandler<ServerTimeQuery, (string utc, string korea, string koreanCalendar)>
{
    private readonly ITelemetryService _telemetryService;
    
    public ServerTimeQueryHandler(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }
    
    public async Task<(string utc, string korea, string koreanCalendar)>
        HandleAsync(ServerTimeQuery message, CancellationToken cancellationToken = default)
    {
        Activity? parentActivity = Activity.Current;
        using Activity? span = _telemetryService.StartActivity(nameof(ServerTimeQueryHandler), ActivityKind.Internal, parentActivity?.Context);
        
        await Task.CompletedTask;
        
        Instant now = SystemClock.Instance.GetCurrentInstant();
        
        ZonedDateTime nowInIsoUtc = now.InUtc();
        
        var koreaTimeZone = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
        ZonedDateTime koreaTime = now.InZone(koreaTimeZone);

        // 한국 음력 변환
        DateTime gregDateTime = koreaTime.ToDateTimeOffset().DateTime;
        KoreanLunisolarCalendar koreanLunar = new KoreanLunisolarCalendar();
        
        int lunarYear = koreanLunar.GetYear(gregDateTime);
        int lunarMonth = koreanLunar.GetMonth(gregDateTime);
        int lunarDay = koreanLunar.GetDayOfMonth(gregDateTime);
        
        // 윤달 확인 및 실제 월 계산
        bool isLeapMonth = false;
        int actualMonth = lunarMonth;
        
        // 윤달인 경우 처리
        if (koreanLunar.IsLeapMonth(lunarYear, lunarMonth))
        {
            isLeapMonth = true;
            actualMonth = lunarMonth;
        }
        else
        {
            // 윤달이 있는 해인지 확인하고 월 조정
            for (int m = 1; m < lunarMonth; m++)
            {
                if (koreanLunar.IsLeapMonth(lunarYear, m))
                {
                    actualMonth = lunarMonth - 1;
                    break;
                }
            }
        }
        
        string lunarDateString = $"{lunarYear:0000}-{actualMonth:00}{(isLeapMonth ? "(윤)" : "")}-{lunarDay:00} {gregDateTime:HH:mm:ss}";

        return (
            nowInIsoUtc.ToDateTimeUtc().ToString("O", CultureInfo.InvariantCulture),
            koreaTime.ToDateTimeOffset().ToString("O", CultureInfo.InvariantCulture),
            lunarDateString);
    }
}