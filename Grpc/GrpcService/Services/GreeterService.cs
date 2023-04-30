using Grpc.Core;

namespace GrpcService.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            var userAgent = context.RequestHeaders.GetValue("user-agent");
            string logMessage = $"FROM:{context.Peer} TO:{context.Host} user-agent:{userAgent} Status:{context.Status} Message: Saying hello to {request.Name}";
            _logger.LogInformation(logMessage);
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}