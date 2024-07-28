namespace WebDemo.Domain.Extentions;

public static class DictionaryExtention
{
    public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
    }
}
