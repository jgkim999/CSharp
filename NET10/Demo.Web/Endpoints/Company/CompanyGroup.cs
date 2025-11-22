using FastEndpoints;

namespace Demo.Web.Endpoints.Company;

/// <summary>
/// Company 그룹 엔드포인트
/// </summary>
public sealed class CompanyGroup : Group
{
    /// <summary>
    /// Company group
    /// </summary>
    public CompanyGroup()
    {
        Configure(
            "",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("Company"));
            });
    }
}
