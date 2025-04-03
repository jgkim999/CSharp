using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

using SQLite;

using System.Collections.Concurrent;
using System.Text;

using Unity.Tools.Models;
using Unity.Tools.Models.Sqlite;

namespace Unity.Tools.Repositories;

public class SqliteBuildCacheDb : IBuildCacheDb
{
    private readonly ILogger _logger;
    private readonly SQLiteConnection _db;
    private readonly DefaultObjectPoolProvider _objectPoolProvider;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public SqliteBuildCacheDb(string dbname, ILogger logger)
    {
        _logger = logger;

        var databasePath = dbname;
        _db = new SQLiteConnection(databasePath);
        _db.CreateTable<BuildCacheDirectory>();
        _db.CreateTable<BuildCacheFile>();

        _objectPoolProvider = new();
        _stringBuilderPool = _objectPoolProvider.CreateStringBuilderPool(16384, 32);
    }

    private string MakeBuildCacheDirectoryQuery(List<BuildCacheDirectory> assetDirectories)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.AppendLine("INSERT INTO BuildCacheDirectory (Id, Name) VALUES");
            for (int i = 0; i < assetDirectories.Count; ++i)
            {
                var item = assetDirectories[i];
                if (i != (assetDirectories.Count - 1))
                    sb.AppendLine($" ('{item.Id}','{item.Name}'),");
                else
                    sb.AppendLine($" ('{item.Id}','{item.Name}')");
            }

            sb.AppendLine($";");
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    public async Task AddDirectoryAsync(Dictionary<int, string> directoryMap, IProgressContext progressContext)
    {
        int commitCount = 0;

        List<BuildCacheDirectory> buildCacheDirectories = new();
        foreach (var directory in directoryMap)
        {
            ++commitCount;
            progressContext.Increment(1);
            try
            {
                var buildCacheDirectory = new BuildCacheDirectory() { Id = directory.Key, Name = directory.Value };
                buildCacheDirectories.Add(buildCacheDirectory);

                // 트랜잭션이 너무 커지지 않도록 주기적으로 커밋
                if (commitCount % 100 == 0)
                {
                    string insertQuery1 = MakeBuildCacheDirectoryQuery(buildCacheDirectories);
                    _db.Execute(insertQuery1);
                    buildCacheDirectories.Clear();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add directories in bulk");
            }
        }

        // 마지막 남은 데이터 처리
        string insertQuery2 = MakeBuildCacheDirectoryQuery(buildCacheDirectories);
        _db.Execute(insertQuery2);
        await Task.CompletedTask;
    }

    private string MakeCacheFileQuery(List<BuildCacheFile> cacheFiles)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.AppendLine($"INSERT INTO BuildCacheFile (DirId, Filename) VALUES");
            for (int i = 0; i < cacheFiles.Count; ++i)
            {
                var item = cacheFiles[i];
                if (i != (cacheFiles.Count - 1))
                    sb.AppendLine($" ({item.DirId}, '{item.Filename}'),");
                else
                    sb.AppendLine($" ({item.DirId}, '{item.Filename}')");
            }

            sb.AppendLine(";");
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    public async Task AddFileAsync(ConcurrentBag<UnityCacheFileInfo> files, IProgressContext progressContext)
    {
        int commitCount = 0;
        List<BuildCacheFile> insertFiles = new();
        
        foreach (var file in files)
        {
            ++commitCount;
            progressContext.Increment(1);
            try
            {
                if (string.IsNullOrEmpty(file.Filename))
                    continue;
                var insertObj = new BuildCacheFile()
                {
                    DirId = file.DirNum,
                    Filename = file.Filename
                };
                insertFiles.Add(insertObj);

                // 주기적으로 커밋
                if (commitCount % 100 == 0)
                {
                    string assetQuery1 = MakeCacheFileQuery(insertFiles);
                    _db.Execute(assetQuery1);
                    insertFiles.Clear();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add cache file in bulk");
            }
        }

        // 마지막 남은 데이터 커밋
        string assetQuery2 = MakeCacheFileQuery(insertFiles);
        _db.Execute(assetQuery2);
        await Task.CompletedTask;
    }
}
