using System.Diagnostics;

using GamePulse.Services;
using GamePulse.Sod.Commands;

namespace GamePulse.Sod.Endpoints.Rtt;

/// <summary>
/// 
/// </summary>
public class RttCommand : SodCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientIp"></param>
    /// <summary>
    /// Initializes a new instance of the <see cref="RttCommand"/> class with the specified client IP address and optional parent activity.
    /// </summary>
    /// <param name="clientIp">The IP address of the client initiating the command.</param>
    /// <param name="parentActivity">An optional parent <see cref="Activity"/> for tracing context.</param>
    public RttCommand(string clientIp, Activity? parentActivity) : base(clientIp, parentActivity)
    {
        ClientIp = clientIp;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <summary>
    /// Executes the RTT command asynchronously, logging the client IP and initiating an activity span.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var logger = serviceProvider.GetService<ILogger<RttCommand>>();
        var ipToNationService = serviceProvider.GetService<IIpToNationService>();
        using var span = GamePulseActivitySource.StartActivity(nameof(RttCommand), ActivityKind.Internal, parentActivity: ParentActivity);

        var faker = new Bogus.Faker();
        //ClientIp = faker.Internet.Ip();
        ClientIp = $"{faker.Random.Number(211, 212)}.{faker.Random.Number(185, 186)}.{faker.Random.Number(5, 6)}.{faker.Random.Number(45, 46)}";
        
        logger?.LogInformation("{ClientIp}", ClientIp);
        span?.AddTag("ClientIp", ClientIp);

        // IP 주소를 국가 코드로 변환
        Debug.Assert(ipToNationService != null, nameof(ipToNationService) + " != null");
        var nationCode = await ipToNationService.GetNationCodeAsync(ClientIp, ct);

        // RTT 처리
        await Task.CompletedTask;
    }
}
