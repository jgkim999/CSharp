using Newtonsoft.Json;

namespace RestSharpPractice.Models;

public class TimeZoneConvertResponse
{
    [JsonProperty("fromTimezone")]
    public string FromTimezone { get; set; }
    [JsonProperty("fromDateTime")] 
    public DateTime FromDateTime { get; set; }
    [JsonProperty("toTimeZone")]
    public string ToTimeZone { get; set; }
    [JsonProperty("conversionResult")] 
    public TimeResponse ConversionResult { get; set; }
}