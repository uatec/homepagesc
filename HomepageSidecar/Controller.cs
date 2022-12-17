using k8s;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomepageSidecar;

public class Controller : BackgroundService
{
    private readonly IKubernetes _client;
    private readonly ConfigBuilder _configBuilder;
    private readonly SidecarOptions _options;

    public Controller(ConfigBuilder configBuilder, IKubernetes client, IOptions<SidecarOptions> options)
    {
        _configBuilder = configBuilder;
        _client = client;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        do
        {
            var result1 = await _client.NetworkingV1.ListIngressForAllNamespacesAsync(cancellationToken: token);
            var flatConfig = await _configBuilder.Build(result1, token);
            var c = flatConfig.Select(g => new Dictionary<string, List<Dictionary<string, Service>>>
                    { { g.Key, g.Value.Select(s => new Dictionary<string, Service> { { s.Key, s.Value } }).ToList() } })
                .ToList();


            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var configOutput = serializer.Serialize(c);

            if (!string.IsNullOrEmpty(_options.OutputLocation))
            {
                Console.WriteLine("Writing to: " + _options.OutputLocation);
                await File.WriteAllTextAsync(_options.OutputLocation, configOutput, token);
            }
            else
            {
                Console.WriteLine(configOutput);
            }

            await Task.Delay(10000, token);
        } while (!token.IsCancellationRequested);
    }
}