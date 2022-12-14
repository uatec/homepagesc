namespace HomepageSidecar;

public class Widget
{
    public Widget(string type, string url, string? key)
    {
        Type = type;
        Url = url;
        Key = key;
    }

    public string Type { get; }
    public string Url { get; }
    public string? Key { get; }
}