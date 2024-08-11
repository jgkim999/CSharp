using Consul;

using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Services;
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
        var hosts = new Hosts().Discover();
        var docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        if (docker is not null)
        {
            var containers = docker.GetRunningContainers();
            foreach (var container in containers)
            {
                _logger.LogInformation($"{container.Id} {container.Image}");
                foreach (var network in container.GetNetworks())
                {
                    _logger.LogInformation($"{network.Id} {network.DockerHost}");
                }
            }
            //var networkSettings = docker.GetConfiguration().NetworkSettings;
            //Environment.SetEnvironmentVariable("NetworkSettings", networkSettings.Networks.First().Value.IPAddress);
        }

        var domainName = Environment.UserDomainName;
        var hostName = Dns.GetHostName();
        var ip = (await Dns.GetHostEntryAsync(hostName, cancellationToken)).AddressList
            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
        var networkSettings = Environment.GetEnvironmentVariable("NetworkSettings");

        var registration = new AgentServiceRegistration
        {
            ID = $"webDemoId_{Guid.NewGuid()}",
            Name = "webDemoService",
            Meta = new Dictionary<string, string>
            {
                {"DomainName", domainName},
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
                {"guid", Guid.NewGuid().ToString()},
                {"networkSettings", networkSettings ?? ""}
            }
        };
        
        var check = new AgentServiceCheck
        {
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
            HTTP = "/healthz",
            Interval = TimeSpan.FromSeconds(15),
            Timeout = TimeSpan.FromSeconds(15)
        };
        registration.Checks = new[] { check };

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
