using Client.Enums;

namespace WpfClient.Models
{
    public class LogModel
    {
        public long Id { get; set; }
        public LogType LogType { get; set; }
        public string Message { get; set; }
    }
}
