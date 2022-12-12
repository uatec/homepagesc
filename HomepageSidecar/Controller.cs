using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace HomepageSidecar;

public class Controller  
{
    private readonly Kubernetes _client;

    public Controller(Kubernetes client)
    {
        _client = client;
    }

    public async Task StartAsync(CancellationToken token)
    {
        do
        {
            var result1 = await _client.ListIngressForAllNamespaces1Async(cancellationToken: token);
            Dictionary<string, Dictionary<string, Service>> flatConfig = new Dictionary<string, Dictionary<string, Service>>();
            foreach ( var ingress in result1.Items)
            {
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
                                    Ping = path.Backend.Service != null ? $"http://{path.Backend.Service.Name}.{ingress.Metadata.NamespaceProperty}.svc.cluster.local:{path.Backend.Service.Port.Number ?? 80}" : null
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
                .Build();
            var configOutput = serializer.Serialize(c);
            Console.WriteLine(configOutput);
            
            File.WriteAllText("/app/config/services.yaml", configOutput);
            
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