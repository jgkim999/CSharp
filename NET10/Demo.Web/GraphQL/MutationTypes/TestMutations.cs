using System.Diagnostics;
using Demo.Application.Models;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Web.GraphQL.Types.Input;
using Demo.Web.GraphQL.Types.Payload;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.MutationTypes;

public class TestMutations
{
    /// <summary>
    /// PublishMqAnyMessageAsync 뮤테이션 정의
    /// RabbitMQ Any 방식(round-robin)으로 메시지를 발행합니다
    /// </summary>
    /*
    mutation {
        publishMqAnyMessage(input: { message: "테스트 메시지" }) {
            message
            traceId
            spanId
            timestamp
            errors
        }
    }
     */
    public async Task<TestMqPayload> PublishMqAnyMessageAsync(
        MqMessageInput input,
        IMqPublishService mqPublishService,
        ITelemetryService telemetryService,
        ILogger<TestMutations> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetryService.StartActivity("test.mutations.publish.mq.any");
            await mqPublishService.PublishMessagePackAnyAsync("consumer-any-queue", input.Message, cancellationToken);

            return new TestMqPayload(
                "MQ 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
                Activity.Current?.TraceId.ToString(),
                Activity.Current?.SpanId.ToString(),
                DateTimeOffset.UtcNow.ToString("O")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MQ Any 메시지 발행 중 예외 발생");
            telemetryService.SetActivityError(Activity.Current, ex);
            return new TestMqPayload(null, null, null, null, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// PublishMqMultiMessageAsync 뮤테이션 정의
    /// RabbitMQ Multi 방식(fanout)으로 메시지를 발행합니다
    /// </summary>
    /*
    mutation {
        publishMqMultiMessage(input: { message: "멀티 메시지" }) {
            message
            traceId
            spanId
            timestamp
            errors
        }
    }
     */
    public async Task<TestMqPayload> PublishMqMultiMessageAsync(
        MqMessageInput input,
        IMqPublishService mqPublishService,
        ITelemetryService telemetryService,
        ILogger<TestMutations> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetryService.StartActivity("test.mutations.publish.mq.multi");
            await mqPublishService.PublishMessagePackMultiAsync("consumer-multi-exchange", input.Message, cancellationToken);

            return new TestMqPayload(
                "MQ 테스트가 완료되었습니다.",
                Activity.Current?.TraceId.ToString(),
                Activity.Current?.SpanId.ToString(),
                DateTimeOffset.UtcNow.ToString("O")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MQ Multi 메시지 발행 중 예외 발생");
            telemetryService.SetActivityError(Activity.Current, ex);
            return new TestMqPayload(null, null, null, null, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// PublishMqMessagePackAsync 뮤테이션 정의
    /// RabbitMQ MessagePack 방식으로 타입 정보와 함께 메시지를 발행합니다
    /// </summary>
    /*
    mutation {
        publishMqMessagePack(input: { message: "MessagePack 메시지" }) {
            message
            messageType
            traceId
            spanId
            timestamp
            errors
        }
    }
     */
    public async Task<TestMqMessagePackPayload> PublishMqMessagePackAsync(
        MqMessageInput input,
        IMqPublishService mqPublishService,
        ITelemetryService telemetryService,
        ILogger<TestMutations> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetryService.StartActivity("test.mutations.publish.mq.messagepack");

            var request = new { Message = input.Message };
            await mqPublishService.PublishMessagePackMultiAsync("consumer-multi-exchange", request, cancellationToken);

            return new TestMqMessagePackPayload(
                "MessagePack MQ 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
                request.GetType().FullName,
                Activity.Current?.TraceId.ToString(),
                Activity.Current?.SpanId.ToString(),
                DateTimeOffset.UtcNow.ToString("O")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MQ MessagePack 메시지 발행 중 예외 발생");
            telemetryService.SetActivityError(Activity.Current, ex);
            return new TestMqMessagePackPayload(null, null, null, null, null, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// PublishMqRequestResponseAsync 뮤테이션 정의
    /// RabbitMQ 요청-응답 패턴으로 메시지를 발행하고 응답을 대기합니다
    /// </summary>
    /*
    mutation {
        publishMqRequestResponse(input: { message: "요청-응답 테스트" }) {
            success
            requestId
            responseId
            target
            processingTime
            timestamp
            errors
        }
    }
     */
    public async Task<TestMqRequestResponsePayload> PublishMqRequestResponseAsync(
        MqMessageInput input,
        IMqPublishService mqPublishService,
        ITelemetryService telemetryService,
        ILogger<TestMutations> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetryService.StartActivity("test.mutations.publish.mq.request.response");

            var request = new TestRequest
            {
                Id = Ulid.NewUlid().ToString(),
                Message = input.Message ?? "MessagePack 요청-응답 테스트",
                Timestamp = DateTime.Now,
                Data = new Dictionary<string, object>
                {
                    { "환경", Environment.MachineName },
                    { "프로세스ID", Environment.ProcessId },
                    { "요청시간", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };

            var target = "consumer-any-queue";

            logger.LogInformation(
                "MessagePack 요청-응답 테스트 시작. 대상: {Target}, 요청ID: {RequestId}",
                target, request.Id);

            var response = await mqPublishService.PublishMessagePackAndWaitForResponseAsync<TestRequest, TestResponse>(
                target,
                request,
                TimeSpan.FromSeconds(30),
                cancellationToken);

            logger.LogInformation(
                "MessagePack 응답 수신 완료. 요청ID: {RequestId}, 응답ID: {ResponseId}",
                request.Id, response.ResponseId);

            var processingTime = (DateTime.Now - request.Timestamp).TotalMilliseconds;

            return new TestMqRequestResponsePayload(
                true,
                request.Id,
                response.ResponseId,
                target,
                processingTime,
                DateTimeOffset.UtcNow.ToString("O")
            );
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "MessagePack 요청-응답 타임아웃 발생");
            return new TestMqRequestResponsePayload(
                false,
                null,
                null,
                null,
                0,
                DateTimeOffset.UtcNow.ToString("O"),
                new List<string> { "MessagePack 응답 타임아웃이 발생했습니다. 대상 큐가 응답하지 않습니다." }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MessagePack 요청-응답 테스트 중 오류 발생");
            telemetryService.SetActivityError(Activity.Current, ex);
            return new TestMqRequestResponsePayload(
                false,
                null,
                null,
                null,
                0,
                DateTimeOffset.UtcNow.ToString("O"),
                new List<string> { $"오류 발생: {ex.Message}" }
            );
        }
    }
}
