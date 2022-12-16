using System.Text;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace HomepageSidecar;

public class ConfigBuilder
{
    private readonly IKubernetes _kubeClientObject;
    private readonly SidecarOptions _options;

    public ConfigBuilder(IKubernetes kubeClientObject, IOptions<SidecarOptions> sidecarOptions)
    {
        _kubeClientObject = kubeClientObject;
        _options = sidecarOptions.Value;
    }

    private string? Get(V1Ingress ingress, string attributeName)
    {
        return ingress.Metadata.Annotations.ContainsKey(attributeName)
            ? ingress.Metadata.Annotations[attributeName]
            : null;
    }

    public async Task<Dictionary<string, Dictionary<string, Service>>> Build(V1IngressList ingressData,
        CancellationToken token)
    {
        Dictionary<string, Dictionary<string, Service>> flatConfig = new();
        foreach (var ingress in ingressData.Items)
        {
            if ("false".Equals(Get(ingress, "hajimari.io/enable") ?? _options.IncludeByDefault.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)) continue;

            var groupName = ingress.Metadata.Annotations.ContainsKey("hajimari.io/group")
                ? ingress.Metadata.Annotations["hajimari.io/group"]
                : "Default";

            foreach (var rule in ingress.Spec.Rules)
                if (rule.Http != null)
                {
                    int pathNumber = 0;
                    foreach (var path in rule.Http.Paths)
                    {
                        string scheme =
                            ingress.Spec.Tls != null && ingress.Spec.Tls.Any(tls => tls.Hosts.Contains(rule.Host))
                                ? "https"
                                : "http";
                        var url = $"{scheme}://{rule.Host}{path.Path}";
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
                                var secret = await _kubeClientObject.CoreV1.ReadNamespacedSecretAsync(secretParts[1],
                                    secretParts[0], cancellationToken: token);
                                apiKey = Encoding.Default.GetString(secret.Data[secretParts[2]]);
                            }

                            var port = path.Backend.Service.Port.Number;

                            if (port == null)
                            {
                                var service = await _kubeClientObject.CoreV1.ReadNamespacedServiceAsync(
                                    path.Backend.Service.Name,
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
                            Target = target ?? (_options.DefaultTarget != Target.Default  ? _options.DefaultTarget.ToString() : null),
                            Widget = widget
                        };
                        var serviceName = Get(ingress, "hajimari.io/appName") ?? ingress.Metadata.Name;

                        if (pathNumber > 0 )
                        {
                            serviceName += "-" + pathNumber;
                        }
                        
                        pathNumber++;
                        var group = flatConfig.GetOrAdd(groupName, new Dictionary<string, Service>());
                        group[serviceName] = newValue;
                    }
            }
        }

        return flatConfig;
    }
}