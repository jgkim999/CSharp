using FastEndpoints;
using GamePulse.DTO;
using GamePulse.Processors;

namespace GamePulse.EndPoints.User.Create;


/// <summary>
/// Creates a new user
/// </summary>
public class CreateEndpointV2 : Endpoint<MyRequest, MyResponse>
{
    /// <summary>
    /// Configure
    /// </summary>
    public override void Configure()
    {
        Version(2).StartingRelease(2);
        Post("/api/user/create");
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
        await Send.OkAsync(new()
        {
            FullName = req.FirstName + " " + req.LastName + " v2",
            IsOver18 = req.Age > 18
        });
    }
}
