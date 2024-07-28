using System.Text;
using Consul;
using Microsoft.AspNetCore.Mvc;
using WebDemo.Domain.Extentions;

namespace WebDemo.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConsulClient _consulClient;
        
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConsulClient consulClient)
        {
            _logger = logger;
            _consulClient = consulClient;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet]
        public async Task<string> Get2Async()
        {
            var queryResult = await _consulClient.Agent.Services();
            queryResult.Response.TryGetValue("webDemoId", out var service);
            return service.ToString();
        }

        [HttpGet]
        public async Task<string> Get3Async()
        {
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
