using System.Collections.Concurrent;

using Unity.Tools.Models;

namespace Unity.Tools.Repositories;

public interface IBuildCacheDb
{
    Task AddDirectoryAsync(Dictionary<int, string> directoryMap, IProgressContext progressContext);
    Task AddFileAsync(ConcurrentBag<UnityCacheFileInfo> files, IProgressContext progressContext);
}
