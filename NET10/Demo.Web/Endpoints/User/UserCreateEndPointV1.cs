using Demo.Application.Commands;
using Demo.Application.Configs;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using Demo.Application.Extensions;
using Microsoft.Extensions.Options;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<UserCreateEndpointV1> _logger;
    private readonly RateLimitConfig _rateLimitConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreateEndpointV1"/> class with the specified command mediator, telemetry service, logger, and rate limit configuration.
    /// </summary>
    public UserCreateEndpointV1(
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<UserCreateEndpointV1> logger,
        IOptions<RateLimitConfig> rateLimitConfig)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
        _rateLimitConfig = rateLimitConfig.Value;
    }

    /// <summary>
    /// Configures the endpoint to handle HTTP POST requests at the route "/api/user/create" and allows anonymous access.
    /// Applies rate limiting based on configuration settings.
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();

        // Rate Limiting 적용: 설정 파일에서 읽어온 값 사용
        if (_rateLimitConfig.UserCreateEndpoint.Enabled)
        {
            Throttle(
                hitLimit: _rateLimitConfig.UserCreateEndpoint.HitLimit,
                durationSeconds: _rateLimitConfig.UserCreateEndpoint.DurationSeconds,
                headerName: _rateLimitConfig.UserCreateEndpoint.HeaderName
            );
        }
    }

    /// <summary>
    /// Processes a user creation request, invoking the user creation command and returning an appropriate HTTP response based on the outcome.
    /// </summary>
    /// <param name="req">The user creation request containing name, email, and password.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    public override async Task HandleAsync(UserCreateRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("user.create");

        try
        {
            // 명령 실행
            var ret = await _commandMediator.SendAsync(
                new UserCreateCommand(req.Name, req.Email, req.Password),
                cancellationToken: ct);

            if (ret.Result.IsFailed)
            {
                // 실패 처리
                var errorMessage = ret.Result.GetErrorMessageAll();
                _logger.LogError("User creation command failed: {ErrorMessage}", errorMessage);
                AddError(errorMessage);
                await Send.ErrorsAsync(500, ct);
            }
            else
            {
                await Send.OkAsync(cancellation: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UserCreateEndpointV1));
            AddError(ex.Message);
            _telemetryService.SetActivityError(activity, ex);
            await Send.ErrorsAsync(500, ct);
        }
    }
}
