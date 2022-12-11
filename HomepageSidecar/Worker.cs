using System.Diagnostics;

namespace HomepageSidecar;

using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;


    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Kubernetes kubeClient = new(KubernetesClientConfiguration.InClusterConfig());
        var ingressController = new Controller(
            kubeClient);

        await Task.WhenAll(
            ingressController.StartAsync(stoppingToken)
        ).ConfigureAwait(false);
    }
}
