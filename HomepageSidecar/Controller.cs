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
            var result1 = await _client.ListIngressForAllNamespaces1Async(cancellationToken: token);
            Dictionary<string, Dictionary<string, Service>> flatConfig = new Dictionary<string, Dictionary<string, Service>>();
            foreach ( var ingress in result1.Items)
            {
                if ("false".Equals(Get(ingress, "hajimari.io/enable"), StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                
                string groupName = ingress.Metadata.Annotations.ContainsKey("hajimari.io/group") ?
                    ingress.Metadata.Annotations["hajimari.io/group"] : "Default";
                
                foreach ( var rule in ingress.Spec.Rules ) {
                    if (rule.Http != null)
                    {
                        foreach ( var path in rule.Http.Paths )
                        {
                            
                            var newValue = new Service($"https://{rule.Host}{path.Path}")
                                {
                                    Description = Get(ingress, "hajimari.io/description", ingress.Metadata.Name),
                                    Icon = Get(ingress, "hajimari.io/icon"),
                                    Ping = Get(ingress, "hajimari.io/healthCheck"), 
                                    Target = _options.DefaultTarget.ToString()
                                };
                            string serviceName = Get(ingress, "hajimari.io/appName", ingress.Metadata.Name);
                            
                            var group = flatConfig.GetOrAdd(groupName, new Dictionary<string, Service>());
                            group[serviceName] = newValue;
                        }
                    }
                }
            }

            var c = flatConfig.Select(g => new Dictionary<string, List<Dictionary<string, Service>>> { { g.Key, g.Value.Select(s => new Dictionary<string, Service>{ { s.Key, s.Value}}).ToList() } }).ToList();
            

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var configOutput = serializer.Serialize(c);
            Console.WriteLine(configOutput);

            if (!String.IsNullOrEmpty(_options.OutputLocation))
            {
                Console.WriteLine("Outputting to: " + _options.OutputLocation);
                File.WriteAllText(_options.OutputLocation, configOutput);
            }
            
            await Task.Delay(10000, token);
        } while (!token.IsCancellationRequested);
    }
    

    private string Get(V1Ingress ingress, string attributeName, string otherwise = "")
    {
        return ingress.Metadata.Annotations.ContainsKey(attributeName)
            ? ingress.Metadata.Annotations[attributeName]
            : otherwise;
    }
}