using Quartz;

namespace WebDemo.QuartzJob
{
    public class OnceJob : IJob
    {
        private readonly ILogger<OnceJob> _logger;

        public OnceJob(ILogger<OnceJob> logger)
        {
            _logger = logger;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Job Execute {DateTime.Now}");
            await Task.CompletedTask;
        }
    }
}
