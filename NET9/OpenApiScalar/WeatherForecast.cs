using System.ComponentModel;

namespace OpenApiScalar
{
    public class WeatherForecast
    {
        /// <summary>
        /// ��¥
        /// </summary>
        [Description("��¥�ð�")]
        public DateOnly Date { get; set; }
        /// <summary>
        /// �µ� C
        /// </summary>
        [Description("�µ� C")]
        public int TemperatureC { get; set; }
        /// <summary>
        /// �µ� F
        /// </summary>
        [Description("�µ� F")]
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        /// <summary>
        /// ���
        /// </summary>
        [Description("���")]
        public string? Summary { get; set; }
    }
}
