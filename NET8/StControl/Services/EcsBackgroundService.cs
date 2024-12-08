using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using Task = System.Threading.Tasks.Task;

namespace StControl.Services;

public class EcsBackgroundService : BackgroundService
{
    private readonly ILogger<EcsBackgroundService> _logger;
    private readonly AmazonECSClient _ecsClient;
    private readonly string _cluster = "default";

    public EcsBackgroundService(ILogger<EcsBackgroundService> logger)
    {
        _logger = logger;
        _ecsClient = new AmazonECSClient(RegionEndpoint.APNortheast2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background service is running at: {time}", DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm:ss zzz"));
            try
            {
                var req = new ListTasksRequest
                {
                    Cluster = _cluster
                };
                var res = await _ecsClient.ListTasksAsync(req, stoppingToken);
                if (res == null)
                {
                    _logger.LogInformation("ListTasksAsync null");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                var taskArns = res.TaskArns;
                if (taskArns == null || taskArns.Count == 0)
                {
                    _logger.LogInformation("No tasks found");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                var describeReq = new DescribeTasksRequest
                {
                    Cluster = _cluster,
                    Tasks = taskArns
                };
                var describeRes = await _ecsClient.DescribeTasksAsync(describeReq, stoppingToken);
                if (describeRes == null)
                {
                    _logger.LogInformation("DescribeTasksAsync null");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                foreach (var task in describeRes.Tasks)
                {
                    _logger.LogInformation("Task: {task}", task.TaskArn);
                    _logger.LogInformation("  Last Status: {status}", task.LastStatus);
                    _logger.LogInformation("  Desired Status: {status}", task.DesiredStatus);
                    _logger.LogInformation("  Launch Type: {type}", task.LaunchType);
                    _logger.LogInformation("  Started At: {time}", task.StartedAt);
                    _logger.LogInformation("  Stopped At: {time}", task.StoppedAt);
                    _logger.LogInformation("  Task Definition: {definition}", task.TaskDefinitionArn);
                    _logger.LogInformation("  Container Instance: {instance}", task.ContainerInstanceArn);
                    _logger.LogInformation("  Group: {group}", task.Group);
                    _logger.LogInformation("  Health Status: {status}", task.HealthStatus);

                    task.Containers.ForEach(container =>
                    {
                        _logger.LogInformation("  Container: {name}", container.Name);
                        _logger.LogInformation("    Last Status: {status}", container.LastStatus);
                        _logger.LogInformation("    Exit Code: {code}", container.ExitCode);
                        _logger.LogInformation("    Reason: {reason}", container.Reason);
                        _logger.LogInformation("    Task: {task}", container.TaskArn);
                        _logger.LogInformation("    Image: {image}", container.Image);
                        _logger.LogInformation("    Health Status: {status}", container.HealthStatus);
                        _logger.LogInformation("    CPU: {cpu}", container.Cpu);
                        _logger.LogInformation("    Memory: {memory}", container.Memory);
                        _logger.LogInformation("    Network: {network}", container.NetworkInterfaces);
                        _logger.LogInformation("    Network: {network}", container.NetworkBindings);
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogException(e);
                await Task.Delay(5000, stoppingToken); // Delay for 5 seconds before the next iteration
            }
        }
    }
}
