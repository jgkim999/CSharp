using Grpc.Core;
using Grpc.Core.Interceptors;

namespace GrpcService.Services
{
    /// <summary>
    /// https://learn.microsoft.com/ko-kr/aspnet/core/grpc/interceptors?view=aspnetcore-7.0
    /// </summary>
    public class ServerLoggerInterceptor : Interceptor
    {
        private readonly ILogger _logger;

        public ServerLoggerInterceptor(ILogger<ServerLoggerInterceptor> logger)
        {
            _logger = logger;
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                string logMessage = $"AsyncClientStreamingCall. Method:{context.Method} Host:{context.Host}";
                _logger.LogInformation(logMessage);
                return continuation(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                string logMessage = $"AsyncDuplexStreamingCall. Method:{context.Method} Host:{context.Host}";
                _logger.LogInformation(logMessage);
                return continuation(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                string logMessage = $"AsyncServerStreamingCall. Method:{context.Method} Host:{context.Host}";
                _logger.LogInformation(logMessage);
                return continuation(request, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                string logMessage = $"AsyncUnaryCall. Method:{context.Method} Host:{context.Host}";
                _logger.LogInformation(logMessage);
                return continuation(request, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                string logMessage = $"BlockingUnaryCall. Method:{context.Method} Host:{context.Host}";
                _logger.LogInformation(logMessage);
                return continuation(request, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var userAgent = context.RequestHeaders.GetValue("user-agent");
                string logMessage = $"Starting receiving call. Type:{MethodType.ClientStreaming} Method:{context.Method} Peer:{context.Peer} Host:{context.Host} user-agent:{userAgent} Status:{context.Status}";
                _logger.LogInformation(logMessage);
                return await continuation(requestStream, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var userAgent = context.RequestHeaders.GetValue("user-agent");
                string logMessage = $"Starting receiving call. Type:{MethodType.DuplexStreaming} Method:{context.Method} Peer:{context.Peer} Host:{context.Host} user-agent:{userAgent} Status:{context.Status}";
                _logger.LogInformation(logMessage);
                await continuation(requestStream, responseStream, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var userAgent = context.RequestHeaders.GetValue("user-agent");
                string logMessage = $"Starting receiving call. Type:{MethodType.ServerStreaming} Method:{context.Method} Peer:{context.Peer} Host:{context.Host} user-agent:{userAgent} Status:{context.Status}";
                _logger.LogInformation(logMessage);
                await continuation(request, responseStream, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var userAgent = context.RequestHeaders.GetValue("user-agent");
                string logMessage = $"Starting receiving call. Type:{MethodType.Unary} Method:{context.Method} Peer:{context.Peer} Host:{context.Host} user-agent:{userAgent} Status:{context.Status}";
                _logger.LogInformation(logMessage);
                return await continuation(request, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error thrown by {context.Method}.");
                throw;
            }
        }
    }
}
