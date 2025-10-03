using FastEndpoints;

namespace Demo.Web.Endpoints.Company;

public class CompanyGroup : Group
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
