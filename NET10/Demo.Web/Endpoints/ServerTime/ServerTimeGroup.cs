using FastEndpoints;

namespace Demo.Web.Endpoints.ServerTime;

public class ServerTimeGroup : Group
{
    /// <summary>
    /// ServerTime group
    /// </summary>
    public ServerTimeGroup()
    {
        Configure(
            "",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("ServerTime"));
            });
    }
}