using Newtonsoft.Json;

namespace RestSharpPractice.Models;

public class TimeConvertRequest
{
    [JsonProperty("fromTimeZone")]
    public string FromTimeZone { get; set; }
    [JsonProperty("dateTime")]
    public string DateTime { get; set; }
    [JsonProperty("toTimeZone")]
    public string ToTimeZone { get; set; }
    [JsonProperty("dstAmbiguity")]
    public string DstAmbiguity { get; set; }
}