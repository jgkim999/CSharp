namespace Client.Interfaces
{
    public interface INetworkStatistics
    {
        void AddReceive(long received);
        void AddSent(long size);
    }
}
