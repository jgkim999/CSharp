using System.Collections.Concurrent;

using Unity.Tools.Models;
using Unity.Tools.Models.Sqlite;

namespace Unity.Tools.Repositories;

public interface IBuildCacheDb
{
    Task AddDirectoryAsync(Dictionary<int, string> directoryMap, IProgressContext progressContext);
    Task AddFileAsync(ConcurrentBag<UnityCacheFileInfo> files, IProgressContext progressContext);
    Task<List<BuildCacheFileDirectory>> GetFilesAsync();
}
