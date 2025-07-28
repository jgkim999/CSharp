using FastEndpoints;
using GamePulse.DTO;
using GamePulse.Processors;
using OpenTelemetry.Trace;

namespace GamePulse.EndPoints.User.Create;

/// <summary>
/// Creates a new user
/// </summary>
public class CreateEndpointV1 : Endpoint<MyRequest, MyResponse>
{
    private readonly Tracer _tracer;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tracer"></param>
    public CreateEndpointV1(Tracer tracer)
    {
        _tracer = tracer;
    }
    
    /// <summary>
    /// Configure
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/create");
        Version(1).StartingRelease(1).DeprecateAt(2);
        PreProcessor<ValidationErrorLogger<MyRequest>>();
        AllowAnonymous();
        Summary(s => {
            s.Summary = "사용자 생성";
            s.Description = "입력한 정보를 바탕으로 사용자를 생성합니다.";
            s.Response<MyResponse>(200, "User created successfully");
        });
    }

    /// <summary>
    /// Handles user creation request
    /// </summary>
    /// <param name="req">User creation request</param>
    /// <param name="ct">Cancellation token</param>
    public override async Task HandleAsync(MyRequest req, CancellationToken ct)
    {
        using var span = _tracer.StartActiveSpan(nameof(CreateEndpointV1));
        
        await Send.OkAsync(new()
        {
            FullName = req.FirstName + " " + req.LastName,
            IsOver18 = req.Age > 18
        });
    }
}
