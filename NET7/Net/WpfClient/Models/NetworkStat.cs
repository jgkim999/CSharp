using System;

namespace WpfClient.Models
{
    public class NetworkStat
    {
        public DateTime At { get; set; }
        public long UnixMinutes { get; set; }
        public long Sent { get; set; }
        public long Received { get; set; }
    }
}
