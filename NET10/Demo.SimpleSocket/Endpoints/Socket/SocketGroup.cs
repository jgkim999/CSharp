using FastEndpoints;

namespace Demo.SimpleSocket.Endpoints.Socket;

public sealed class SocketGroup : Group
{
    public SocketGroup()
    {
        Configure(
            "socket",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("Socket"));
            });
    }
}
