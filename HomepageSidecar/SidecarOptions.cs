public class SidecarOptions
{
    public bool InCluster { get; set; }
    public string OutputLocation { get; set; }
    public Target DefaultTarget { get; set; }
}

public enum Target
{
    _blank,
    _self,
    _top
}