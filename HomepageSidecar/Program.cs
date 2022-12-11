using HomepageSidecar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) => { services.AddHostedService<Worker>(); })
    .Build()
    .Run();
