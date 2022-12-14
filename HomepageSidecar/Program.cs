using HomepageSidecar;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Controller>();
        services.AddSingleton<IKubernetes>(s =>
        {
            var options = s.GetService<IOptions<SidecarOptions>>();
            if (options.Value.InCluster)
                return new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
            return new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile());
        });
        services.Configure<SidecarOptions>(
            hostContext.Configuration);
    })
    .Build()
    .Run();