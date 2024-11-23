using System.Diagnostics;
using System.Text;
using Consul;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using WebDemo.Application;
using WebDemo.Application.Repositories;
using WebDemo.Application.WeatherService;
using WebDemo.Domain.Extentions;
using WebDemo.Domain.Models;

namespace WebDemo.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConsulClient _consulClient;
        private readonly IWeatherForecastRepository _weatherForecastRepo;
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ActivityManager _activityManager;
        private readonly IMediator _mediator;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            //IConsulClient consulClient,
            ActivityManager activityManager,
            IWeatherForecastRepository weatherForecastRepo,
            IMediator mediator)
        {
            _logger = logger;
            //_consulClient = consulClient;
            _activityManager = activityManager;
            _mediator = mediator;
            _weatherForecastRepo = weatherForecastRepo;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            HttpContext.Response.Headers.Add("Request-id", Activity.Current?.TraceId.ToString() ?? string.Empty);

            var traceId = Activity.Current?.TraceId.ToString() ?? ControllerContext.HttpContext.TraceIdentifier;
            GlobalLogger.GetLogger<WeatherForecastController>().Information("TraceIdentifier:{traceId}", traceId);
            using Activity? activity = _activityManager.StartActivity(nameof(WeatherForecast));
            activity?.SetParentId(traceId);
            return await _weatherForecastRepo.GetAsync(traceId);
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Mediator()
        {
            HttpContext.Response.Headers.Add("Request-id", Activity.Current?.TraceId.ToString() ?? string.Empty);

            /*
            var queryResult = await _consulClient.Agent.Services();
            queryResult.Response.TryGetValue("webDemoId", out var service);
            return service.ToString();
            */
            var traceId = Activity.Current?.TraceId.ToString() ?? ControllerContext.HttpContext.TraceIdentifier;
            using Activity? activity = _activityManager.StartActivity(nameof(WeatherForecast));
            activity?.SetParentId(traceId);
            GlobalLogger.GetLogger<WeatherForecastController>(activity).Information("traceId:{traceId}", traceId);
            return await _mediator.Send(new WeatherRequest(activity?.TraceId.ToString()));
            //return await _weatherForecastRepo.GetAsync(activity);
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> MassTransit()
        {
            HttpContext.Response.Headers.Add("Request-id", Activity.Current?.TraceId.ToString() ?? string.Empty);

            string traceId = Activity.Current?.TraceId.ToString() ?? ControllerContext.HttpContext.TraceIdentifier;
            using var activity = _activityManager.StartActivity(nameof(WeatherForecast));
            activity?.SetParentId(traceId);
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.route", "/WeatherForecast/MassTransit");
            activity?.SetTag("http.status_code", 200);

            GlobalLogger.GetLogger<WeatherForecastController>(activity).Information("traceId:{traceId}", traceId);

            return await _mediator.Send(new WeatherMassTransitRequest(traceId));
        }

        [HttpGet]
        public async Task<string> Get3Async()
        {
            HttpContext.Response.Headers.Add("Request-id", Activity.Current?.TraceId.ToString() ?? string.Empty);

            StringBuilder sb = new();
            var queryResult = await _consulClient.Catalog.Services();
            var services = queryResult.Response;
            foreach (var service in services)
            {
                var serviceNodeRes = await _consulClient.Catalog.Service(service.Key);
                CatalogService[] nodes = serviceNodeRes.Response;
                foreach (CatalogService node in nodes)
                {
                    sb.AppendLine($"Node:{node.Node} Address:{node.Address} ServiceID:{node.ServiceID}");
                    sb.AppendLine($"{node.ServiceName} {node.ServiceAddress} {node.ServicePort}");
                    if (node.ServiceTaggedAddresses is not null)
                        sb.AppendLine($"{node.ServiceTaggedAddresses.ToDebugString()}");
                    sb.AppendLine($"{node.ServiceMeta.ToDebugString()}");
                }
                /*
                sb.AppendLine($"Services:{services.ToString()}");
                var nodes = await _consulClient.Catalog.Nodes(new QueryOptions()
                {
                    Near = service.Key
                });
                foreach (var node in nodes.Response)
                {
                    sb.AppendLine($"Node:{node.ToString()}");

                    var cn = await _consulClient.Catalog.Node(node.Name);
                    sb.AppendLine($"Node:{cn.Response.ToString()}");
                }
                */
            }
            return sb.ToString();
        }
    }
}
