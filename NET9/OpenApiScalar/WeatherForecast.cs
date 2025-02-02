using System.ComponentModel;

namespace OpenApiScalar
{
    public class WeatherForecast
    {
        /// <summary>
        /// 날짜
        /// </summary>
        [Description("날짜시간")]
        public DateOnly Date { get; set; }
        /// <summary>
        /// 온도 C
        /// </summary>
        [Description("온도 C")]
        public int TemperatureC { get; set; }
        /// <summary>
        /// 온도 F
        /// </summary>
        [Description("온도 F")]
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        /// <summary>
        /// 요약
        /// </summary>
        [Description("요약")]
        public string? Summary { get; set; }
    }
}
