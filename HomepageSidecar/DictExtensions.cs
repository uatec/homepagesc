namespace HomepageSidecar;

public static class DictExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, TValue defaultValue)
        where TKey : notnull
    {
        if (self.ContainsKey(key)) return self[key];
        return self[key] = defaultValue;
    }
}