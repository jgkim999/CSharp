using Microsoft.AspNetCore.Mvc;

using Quartz;

using WebDemo.Domain.Extentions;
using WebDemo.QuartzJob;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuartzController : ControllerBase
{
    private readonly ILogger<QuartzController> _logger;
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzController(
        ILogger<QuartzController> logger,
        ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    // GET: api/<QuartzController>
    [HttpGet]
    public async Task<string> Get()
    {
        var now = DateTime.Now.AddSeconds(10);
        var nowFmt = now.FormatyyyyMMddHHmmss();
        var localDt = nowFmt.ToDateTime();
        var utcDt = localDt.ToUniversalTime();

        _logger.LogInformation($"Local:{localDt}");
        _logger.LogInformation($"Utc:{utcDt}");

        var scheduler = await _schedulerFactory.GetScheduler();
        var onceJob = JobBuilder.Create<OnceJob>()
            .WithIdentity("job1", "jobGroup")
            .Build();

        var onceTrigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "jobGroup")
            .StartAt(utcDt)
            .WithSimpleSchedule(x => x.WithRepeatCount(0))
            .Build();
        await scheduler.ScheduleJob(onceJob, onceTrigger);
        return nowFmt;
    }

    //// GET api/<QuartzController>/5
    //[HttpGet("{id}")]
    //public string Get(int id)
    //{
    //    return "value";
    //}

    //// POST api/<QuartzController>
    //[HttpPost]
    //public void Post([FromBody] string value)
    //{
    //}

    //// PUT api/<QuartzController>/5
    //[HttpPut("{id}")]
    //public void Put(int id, [FromBody] string value)
    //{
    //}

    //// DELETE api/<QuartzController>/5
    //[HttpDelete("{id}")]
    //public void Delete(int id)
    //{
    //}
}
