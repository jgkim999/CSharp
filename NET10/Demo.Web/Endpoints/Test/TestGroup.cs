using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// Test group
/// </summary>
public class TestGroup : Group
{
    public TestGroup()
    {
        Configure(
            "admin",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("test"));
            });
    }
}

public class MqTest : SubGroup<TestGroup>
{
    public MqTest()
    {
        Configure(
            "Mq Test",
            ep =>
            {
                ep.Description(
                    x => x.Produces(402)
                        .WithTags("mq"));
            });
    }
}

public class LoggingTest : SubGroup<TestGroup>
{
    public LoggingTest()
    {
        Configure(
            "Logging Test",
            ep =>
            {
                ep.Description(
                    x => x.Produces(403)
                        .WithTags("logging"));
            });
    }
}
