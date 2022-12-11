namespace HomepageSidecar;

public class Widget
{
    public Widget(string type, string url, string key)
    {
        this.Type = type;
        this.Url = url;
        this.Key = key;
    }

    public string Type { get; set; }
    public string Url { get; set; }
    public string Key { get; set; }
}