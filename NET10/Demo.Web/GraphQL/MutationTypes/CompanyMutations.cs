using Demo.Application.Commands;
using Demo.Application.Extensions;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Input;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.MutationTypes;

public class CompanyMutations
{
    /// <summary>
    /// CreateCompanyAsync 뮤테이션 정의
    /// 새로운 회사를 생성합니다
    /// </summary>
    /*
    mutation {
        createCompany(input: { name: "삼성전자" }) {
            success
            message
            errors
        }
    }
     */
    public async Task<CreateCompanyPayload> CreateCompanyAsync(
        CreateCompanyInput input,
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<CompanyMutations> logger,
        CancellationToken cancellationToken)
    {
        using var activity = telemetryService.StartActivity("company.mutations.create.company");

        try
        {
            var result = await commandMediator.SendAsync(
                new CompanyCreateCommand(input.Name),
                cancellationToken: cancellationToken);

            if (result.Result.IsFailed)
            {
                var errorMessage = result.Result.GetErrorMessageAll();
                logger.LogError("회사 생성 명령 실패: {ErrorMessage}", errorMessage);
                return new CreateCompanyPayload(false, null, new List<string> { errorMessage });
            }

            logger.LogInformation("회사 생성 성공: {Name}", input.Name);
            return new CreateCompanyPayload(true, "회사가 성공적으로 생성되었습니다.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "회사 생성 중 예외 발생");
            telemetryService.SetActivityError(activity, ex);
            return new CreateCompanyPayload(false, null, new List<string> { ex.Message });
        }
    }
}
