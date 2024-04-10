using Quartz;

namespace WebApiDemo.SchedulingJobs;

public class HelloJob : IJob
{
    private readonly ILogger<HelloJob> _logger;

    public HelloJob(ILogger<HelloJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("HelloJob");
        return Task.CompletedTask;
    }
}
