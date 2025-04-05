using System.Collections.Concurrent;

using Unity.Tools.Models;

namespace Unity.Tools.Repositories;

public interface IDependencyDb
{
    Task AddDirectoryAsync(Dictionary<int, string> directoryMap, IProgressContext progressContext);
    Task AddAssetAsync(ConcurrentBag<UnityMetaFileInfo> assetFiles, IProgressContext progressContext);
}
