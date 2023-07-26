namespace Client.Interfaces
{
    public interface IPackageDispatcher<Package>
    {
        void Dispatch(Package package);
    }
}
