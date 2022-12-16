namespace HomepageSidecar;

public class SidecarOptions
{
    public bool InCluster { get; set; }
    public bool IncludeByDefault { get; set; } = true;
    public string? OutputLocation { get; set; }
    public Target DefaultTarget { get; set; }
}

public enum Target
{
    Default,
    _blank,
    _self,
    _top
}