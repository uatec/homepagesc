namespace HomepageSidecar;

public class Service
{
    public Service(string href)
    {
        Href = href;
    }

    public string? Icon { get; set; }
    public string Href { get; set; }
    public string? Description { get; set; }
    public string? Ping { get; set; }
    public string? Container { get; set; }
    public Widget? Widget { get; set; }
    public string? Target { get; set; }
}