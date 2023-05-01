using Grpc.Core;
using static GrpcService.Example;

namespace GrpcService.Services
{
    public class ExampleService : ExampleBase
    {
        private readonly ILogger<ExampleService> _logger;

        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }

        public override async Task StreamingFromServer(ExampleRequest request, IServerStreamWriter<ExampleResponse> responseStream, ServerCallContext context)
        {
            int count = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(new ExampleResponse() { Message = $"StreamingFromServer {++count}" });
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            }
        }

        public override Task<ExampleResponse> UnaryCall(ExampleRequest request, ServerCallContext context)
        {
            var response = new ExampleResponse()
            {
                Message = $"UnaryCall {request.PageIndex} {request.PageSize} {request.IsDescending}"
            };
            return Task.FromResult(response);
        }

        public override async Task<ExampleResponse> StreamingFromClient(IAsyncStreamReader<ExampleRequest> requestStream, ServerCallContext context)
        {
            int pageIndex = 0;
            int pageSize = 0;
            await foreach (var message in requestStream.ReadAllAsync())
            {
                pageIndex += message.PageIndex;
                pageSize += message.PageSize;
            }
            return new ExampleResponse()
            {
                Message = $"StreamingFromClient pageIndex:{pageIndex} pageSize:{pageSize}"
            };
        }

        public override async Task StreamingBothWays(IAsyncStreamReader<ExampleRequest> requestStream, IServerStreamWriter<ExampleResponse> responseStream, ServerCallContext context)
        {
            int pageIndex = 0;
            // Read requests in a background task.
            var readTask = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    // Process request.
                    pageIndex += message.PageIndex;
                }
            });

            // Send responses until the client signals that it is complete.
            while (!readTask.IsCompleted)
            {
                await responseStream.WriteAsync(new ExampleResponse() { Message = $"StreamingBothWays pageIdex: {pageIndex}" });
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            }
        }
    }
}
