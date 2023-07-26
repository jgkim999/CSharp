namespace WpfClient.Net;

public interface ICustomProtocolHandler
{
    void EchoRes(PKTEcho message);
}
