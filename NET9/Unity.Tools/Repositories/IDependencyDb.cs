namespace Unity.Tools.Repositories;

public interface IDependencyDb
{
    void AddDirectory(Dictionary<int, string> directoryMap, IProgressContext progressContext);
}
