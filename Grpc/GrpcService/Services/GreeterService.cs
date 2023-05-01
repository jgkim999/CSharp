using Grpc.Core;
using static GrpcService.Greeter;

namespace GrpcService.Services
{
    public class GreeterService : GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            string logMessage = $"SayHello. Message:{request.Name}";
            _logger.LogInformation(logMessage);
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}