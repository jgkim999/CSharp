using System.ComponentModel;

namespace Demo.Web.Endpoints.Test;

public class MqPublishRequest
{
    [DefaultValue("Hello MQ")]
    public string Message { get; set; } = "Hello MQ";
}
