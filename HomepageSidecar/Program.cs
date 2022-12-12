using HomepageSidecar;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<IKubernetes>(new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));
    })
    .Build()
    .Run();
