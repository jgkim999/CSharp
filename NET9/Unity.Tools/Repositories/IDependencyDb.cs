using System.Collections.Concurrent;

namespace Unity.Tools.Repositories;

public interface IDependencyDb
{
    void AddDirectory(Dictionary<int, string> directoryMap, IProgressContext progressContext);
    void AddAsset(ConcurrentBag<UnityMetaFileInfo> assetFiles, IProgressContext progressContext);
}
