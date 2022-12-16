using FluentAssertions;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace HomepageSidecar.Tests.Service
{
    public class BuilderTests
    {
        private V1IngressList ingressData;
        
        [SetUp]
        public void BeforeEach()
        {
            ingressData = new V1IngressList
            {
                Items = new List<V1Ingress>
                {
                    new()
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Name = "my-ingress",
                            NamespaceProperty = "my-namespace",
                            Annotations = new Dictionary<string, string>()
                        },
                        Spec = new V1IngressSpec
                        {
                            Rules = new List<V1IngressRule>
                            {
                                new()
                                {
                                    Host = "my-host.com",
                                    Http = new V1HTTPIngressRuleValue(new List<V1HTTPIngressPath>
                                    {
                                        new(
                                            new V1IngressBackend(service: new V1IngressServiceBackend("my-service",
                                                new V1ServiceBackendPort(number: 80))), "Prefix", "/my-path")
                                    })
                                }
                            }
                        }
                    }
                }
            };
        }
        
        [Test]
        public async Task BuildsNameAndPath()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Href.Should()
                .Be("http://my-host.com/my-path", "Should construct the the full path");
        }
        
        [Test]
        public async Task ConfigureGroup()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/group"] = "Some Other Group";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Some Other Group"]["my-ingress"].Href.Should()
                .Be("http://my-host.com/my-path", "Should construct the the full path");
        }
        
        [Test]
        public async Task ConfigureName()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/appName"] = "Some Different Name";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["Some Different Name"].Href.Should()
                .Be("http://my-host.com/my-path", "Should construct the the full path");
        }

        [Test]
        public async Task ConfigureIcon()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/icon"] = "http://awesomeicons.local/some-icon.png";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Icon.Should()
                .Be("http://awesomeicons.local/some-icon.png", "Should populate icon from annotation");
        }

        [Test]
        public async Task ConfigureDescription()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/description"] = "An awesome and interesting description";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Description.Should()
                .Be("An awesome and interesting description", "Should populate icon from annotation");
        }
        
        [Test]
        public async Task ConfigureHealthcheck()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/healthCheck"] = "http://service.namespace.svc.cluster.local";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Ping.Should()
                .Be("http://service.namespace.svc.cluster.local", "Should populate ping from healthcheck annotation");
        }
        
        [Test]
        public async Task HttpsIngress()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            string secureHost = "my-secure-host.com";
            ingressData.Items.Single().Spec.Tls = new List<V1IngressTLS>
            {
                new (new List<string> { secureHost })
            };
            ingressData.Items.Single().Spec.Rules.Single().Host = secureHost;
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Href.Should()
                .Be($"https://{secureHost}/my-path", "Should construct an https path");
        }
        
        [Test]
        public async Task MultiplePaths()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            ingressData.Items = new List<V1Ingress>
            {
                new V1Ingress {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "some-ingress",
                        Annotations = new Dictionary<string, string>()
                    },
                    Spec = new V1IngressSpec {
                        Rules = new List<V1IngressRule> {
                            new V1IngressRule {
                                Host = "some-host.com",
                                Http = new V1HTTPIngressRuleValue {
                                    Paths = new List<V1HTTPIngressPath> {
                                        new V1HTTPIngressPath {
                                            Backend = new V1IngressBackend {
                                                Service = new V1IngressServiceBackend("my-service", new V1ServiceBackendPort(number: 80))
                                            },
                                            Path = "/"
                                        },
                                        new V1HTTPIngressPath {
                                            Backend = new V1IngressBackend {
                                                Service = new V1IngressServiceBackend("my-service", new V1ServiceBackendPort(number: 80))
                                            },
                                            Path = "/sub-path"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["some-ingress"].Href.Should()
                .Be($"http://some-host.com/", "Should create a service for the first path");
            
            config["Default"]["some-ingress-1"].Href.Should()
                .Be($"http://some-host.com/sub-path", "Should create a service for the second path");
        }
        
        [Test]
        public async Task TargetEmptyByDefault()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions()));
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Target.Should().BeNull();
        } 
        
        [Test]
        public async Task TargetConfiguredBySetting()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions { DefaultTarget = Target._top}));
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Target.Should().Be("_top");
        } 
        
        [Test]
        public async Task TargetOverridenByAnnotation()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions { DefaultTarget = Target._top}));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/target"] = "_self";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Target.Should().Be("_self");
        }
        
        [Test]
        public async Task DisabledBySettingOverriddenByAnnotation()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions{IncludeByDefault =  false}));
            ingressData.Items.Single().Metadata.Annotations["hajimari.io/enable"] = "true";
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config["Default"]["my-ingress"].Href.Should().NotBeEmpty();
        }
        
        [Test]
        public async Task DisabledBySetting()
        {
            var kubeClient = new Mock<IKubernetes>(MockBehavior.Default);
            var configBuilder =
                new ConfigBuilder(kubeClient.Object, new OptionsWrapper<SidecarOptions>(new SidecarOptions{IncludeByDefault =  false}));
            var config = await configBuilder.Build(ingressData, CancellationToken.None);
            config.Should().BeEmpty();
        }
    }
}

// WIDGETS
// enable widget
// populate widget key from secret with/without

// OUTPUT
// path configuration

// CONFIGURATION
// incluster flag on and off
