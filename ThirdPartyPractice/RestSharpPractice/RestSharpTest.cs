using System.Net;
using Newtonsoft.Json;
using RestSharp;
using RestSharpPractice.Models;

namespace RestSharpPractice
{
    /// <summary>
    /// https://restsharp.dev/
    /// </summary>
    public class RestSharpTest
    {
        /// <summary>
        /// https://timeapi.io/swagger/index.html
        /// </summary>
        public async Task<TimeResponse?> Get()
        {
            var options = new RestClientOptions("https://timeapi.io/api/Time/current");
            var client = new RestClient(options);
            var request = new RestRequest("zone?timeZone=Asia/Seoul")
            {
                Method = Method.Get,
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("content-type", "application/json");
            // The cancellation token comes from the caller. You can still make a call without it.
            var response = await client.GetAsync(request, new CancellationToken());
            if (response.StatusCode == HttpStatusCode.OK)
            {
                TimeResponse time = JsonConvert.DeserializeObject<TimeResponse>(response.Content);
                Console.WriteLine($"{time.DateTime} {time.TimeZone}");
                return time;
            }
            return null;
        }

        public async Task<TimeResponse> Post(TimeResponse time, string toTimeZoe = "America/Los_Angeles")
        {
            var options = new RestClientOptions("https://timeapi.io/api/Conversion/ConvertTimeZone");
            var client = new RestClient(options);
            var request = new RestRequest()
            {
                Method = Method.Post
            };

            TimeConvertRequest requestTime = new()
            {
                FromTimeZone = time.TimeZone,
                DateTime = time.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ToTimeZone = toTimeZoe,
                DstAmbiguity = ""
            };
            request.AddJsonBody(requestTime);
            // request.AddHeader("content-type", "application/json");
            // request.AddParameter(
            //     "application/json; charset=utf-8", 
            //     JsonConvert.SerializeObject(requestTime),
            //     ParameterType.RequestBody);
            // The cancellation token comes from the caller. You can still make a call without it.
            var response = await client.PostAsync(request, new CancellationToken());
            if (response.StatusCode == HttpStatusCode.OK)
            {
                TimeZoneConvertResponse converTime = JsonConvert.DeserializeObject<TimeZoneConvertResponse>(response.Content);
                Console.WriteLine($"{converTime.ConversionResult.DateTime} {converTime.ConversionResult.TimeZone}");
                return converTime.ConversionResult;
            }
            return null;
        }
    }
}
