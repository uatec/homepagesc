using System.Text;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomepageSidecar;

public class Controller : BackgroundService
{
    private readonly IKubernetes _client;
    private readonly SidecarOptions _options;

    public Controller(IKubernetes client, IOptions<SidecarOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        do
        {
            var result1 = await _client.NetworkingV1.ListIngressForAllNamespacesAsync(cancellationToken: token);
            var flatConfig = new Dictionary<string, Dictionary<string, Service>>();
            foreach (var ingress in result1.Items)
            {
                if ("false".Equals(Get(ingress, "hajimari.io/enable"),
                        StringComparison.InvariantCultureIgnoreCase)) continue;

                var groupName = ingress.Metadata.Annotations.ContainsKey("hajimari.io/group")
                    ? ingress.Metadata.Annotations["hajimari.io/group"]
                    : "Default";

                foreach (var rule in ingress.Spec.Rules)
                    if (rule.Http != null)
                        foreach (var path in rule.Http.Paths)
                        {
                            var url = $"https://{rule.Host}{path.Path}";
                            var widgetType = Get(ingress, "hajimari.io/widget_type");

                            Widget? widget = null;
                            if (!string.IsNullOrEmpty(widgetType))
                            {
                                string? apiKey = null;
                                var apiKeySecretName = Get(ingress, "hajimari.io/widget_secret");
                                if (!string.IsNullOrEmpty(apiKeySecretName))
                                {
                                    var secretParts = apiKeySecretName.Split('/');
                                    // TODO: improved formatting
                                    var secret = await _client.CoreV1.ReadNamespacedSecretAsync(secretParts[1], secretParts[0], cancellationToken: token);
                                    apiKey = Encoding.Default.GetString(secret.Data[secretParts[2]]);
                                }

                                var port = path.Backend.Service.Port.Number;

                                if (port == null)
                                {
                                    var service = await _client.CoreV1.ReadNamespacedServiceAsync(path.Backend.Service.Name,
                                        ingress.Metadata.NamespaceProperty, cancellationToken: token);
                                    port = service.Spec.Ports.Single(p => p.Name == path.Backend.Service.Port.Name)
                                        .Port;
                                }

                                widget = new Widget(widgetType,
                                    $"http://{path.Backend.Service.Name}.{ingress.Metadata.NamespaceProperty}.svc.cluster.local:{port}",
                                    apiKey);
                            }

                            var target = Get(ingress, "hajimari.io/target");

                            var newValue = new Service(url)
                            {
                                Description = Get(ingress, "hajimari.io/description"),
                                Icon = Get(ingress, "hajimari.io/icon"),
                                Ping = Get(ingress, "hajimari.io/healthCheck"),
                                Target = target ?? _options.DefaultTarget.ToString(),
                                Widget = widget
                            };
                            var serviceName = Get(ingress, "hajimari.io/appName") ?? ingress.Metadata.Name;

                            var group = flatConfig.GetOrAdd(groupName, new Dictionary<string, Service>());
                            group[serviceName] = newValue;
                        }
            }

            var c = flatConfig.Select(g => new Dictionary<string, List<Dictionary<string, Service>>>
                    { { g.Key, g.Value.Select(s => new Dictionary<string, Service> { { s.Key, s.Value } }).ToList() } })
                .ToList();


            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var configOutput = serializer.Serialize(c);
            Console.WriteLine(configOutput);

            if (!string.IsNullOrEmpty(_options.OutputLocation))
            {
                Console.WriteLine("Outputting to: " + _options.OutputLocation);
                await File.WriteAllTextAsync(_options.OutputLocation, configOutput, token);
            }

            await Task.Delay(10000, token);
        } while (!token.IsCancellationRequested);
    }


    private string? Get(V1Ingress ingress, string attributeName)
    {
        return ingress.Metadata.Annotations.ContainsKey(attributeName)
            ? ingress.Metadata.Annotations[attributeName]
            : null;
    }
}