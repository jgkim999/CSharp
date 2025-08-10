using Demo.Application.Commands;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using Demo.Application.Extensions;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<UserCreateEndpointV1> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreateEndpointV1"/> class with the specified command mediator and telemetry service.
    /// </summary>
    public UserCreateEndpointV1(
        ICommandMediator commandMediator, 
        ITelemetryService telemetryService,
        ILogger<UserCreateEndpointV1> logger)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }
    
    /// <summary>
    /// Configures the endpoint to handle HTTP POST requests at the route "/api/user/create" and allows anonymous access.
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();
    }

    /// <summary>
    /// Processes a user creation request by sending a command to create a new user and responds with the appropriate HTTP status based on the result.
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
                _logger.LogError(errorMessage);
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
        }
    }
}
