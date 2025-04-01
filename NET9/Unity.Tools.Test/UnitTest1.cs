using Castle.Core.Logging;

using Microsoft.Extensions.Logging;

using Moq;

using Unity.Tools.Repositories;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Unity.Tools.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var mockLogger = new Mock<ILogger>();
        IDependencyDb db = new SqliteDependencyDb("test", mockLogger.Object);
    }
}                                           