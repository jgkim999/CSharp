using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

using Consul;

using WebDemo.Domain.Configs;

namespace WebDemo.Application.Services;

public class ConsulHostedService : IHostedService
{
    private readonly ConsulConfig _config;
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulHostedService> _logger;

    public ConsulHostedService(ConsulConfig config, IConsulClient consulClient, ILogger<ConsulHostedService> logger)
    {
        _config = config;
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hostName = Dns.GetHostName();
        var ip = (await Dns.GetHostEntryAsync(hostName, cancellationToken)).AddressList
            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

        var registration = new AgentServiceRegistration
        {
            ID = $"webDemoId_{Guid.NewGuid()}",
            Name = "webDemoService",
            Meta = new Dictionary<string, string>
            {
                {"HostName", hostName},
                {"ip", ip.ToString()},
                {"port", "5000"},
                {"version", "v1"},
                {"env", "dev"},
                {"tags", "webDemo"},
                {"protocol", "http"},
                {"consul", "true"},
                {"consulAddress", _config.Host},
                {"consulPort", "8500"},
                {"consulScheme", "http"},
                {"consulDatacenter", "dc1"},
                {"guid", Guid.NewGuid().ToString()}
            }
        };
        /*
        var check = new AgentServiceCheck
        {
            HTTP = serviceConfig.HealthCheckUrl,
            Interval = TimeSpan.FromSeconds(serviceConfig.HealthCheckIntervalSeconds),
            Timeout = TimeSpan.FromSeconds(serviceConfig.HealthCheckTimeoutSeconds)
        };
        */
        //registration.Checks = new[] { check };

        _logger.LogInformation($"Registering service with Consul: {registration.Name}");

        await _consulClient.Agent.ServiceDeregister(registration.ID, cancellationToken);
        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var registration = new AgentServiceRegistration { ID = "webDemoId" };

        _logger.LogInformation($"Deregistering service from Consul: {registration.ID}");

        await _consulClient.Agent.ServiceDeregister(registration.ID, cancellationToken);
    }
}
