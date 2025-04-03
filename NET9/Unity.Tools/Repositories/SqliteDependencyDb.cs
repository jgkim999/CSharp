using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

using SQLite;

using System.Collections.Concurrent;
using System.Text;

using Unity.Tools.Models;
using Unity.Tools.Models.Sqlite;

namespace Unity.Tools.Repositories;

public class SqliteDependencyDb : IDependencyDb
{
    private readonly ILogger _logger;
    private readonly SQLiteConnection _db;
    private readonly DefaultObjectPoolProvider _objectPoolProvider;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public SqliteDependencyDb(string dbname, ILogger logger)
    {
        _logger = logger;

        var databasePath = dbname;
        _db = new SQLiteConnection(databasePath);
        _db.CreateTable<AssetDirectory>();
        _db.CreateTable<AssetFile>();
        _db.CreateTable<AssetDependency>();

        _objectPoolProvider = new();
        _stringBuilderPool = _objectPoolProvider.CreateStringBuilderPool(16384, 32);
    }

    private string MakeAssetDirectoryQuery(List<AssetDirectory> assetDirectories)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.AppendLine("INSERT INTO AssetDirectory (Id, Name) VALUES");
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
        
        List<AssetDirectory> assetDirectories = new();
        foreach (var directory in directoryMap)
        {
            ++commitCount;
            progressContext.Increment(1);
            try
            {
                var assetDirectory = new AssetDirectory() { Id = directory.Key, Name = directory.Value };
                assetDirectories.Add(assetDirectory);
                
                // 트랜잭션이 너무 커지지 않도록 주기적으로 커밋
                if (commitCount % 100 == 0)
                {
                    string insertQuery1 = MakeAssetDirectoryQuery(assetDirectories);
                    _db.Execute(insertQuery1);
                    assetDirectories.Clear();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add directories in bulk");
            }
        }

        // 마지막 남은 데이터 처리
        string insertQuery2 = MakeAssetDirectoryQuery(assetDirectories);
        _db.Execute(insertQuery2);
        await Task.CompletedTask;
    }

    private string MakeAssetFileQuery(List<AssetFile> assetFiles)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.AppendLine($"INSERT INTO AssetFile (Guid, DirId, Filename, DependencyCount) VALUES");
            for (int i = 0; i < assetFiles.Count; ++i)
            {
                var item = assetFiles[i];
                if (i != (assetFiles.Count - 1))
                    sb.AppendLine($" ('{item.Guid}', {item.DirId}, '{item.Filename}', {item.DependencyCount}),");
                else
                    sb.AppendLine($" ('{item.Guid}', {item.DirId}, '{item.Filename}', {item.DependencyCount})");
            }

            sb.AppendLine(";");
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    private string MakeDependencyQuery(string sourceGuid, List<string> dependencies)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.AppendLine($"INSERT INTO AssetDependency (Guid, DependencyGuid) VALUES");
            for (int i = 0; i < dependencies.Count; ++i)
            {
                var item = dependencies[i];
                if (i != (dependencies.Count - 1))
                    sb.AppendLine($" ('{sourceGuid}', '{item}'),");
                else
                    sb.AppendLine($" ('{sourceGuid}', '{item}')");
            }

            sb.AppendLine(";");
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    public async Task AddAssetAsync(ConcurrentBag<UnityMetaFileInfo> assetFiles, IProgressContext progressContext)
    {
        int assetCommitCount = 0;
        List<AssetFile> insertAssetFiles = new();
        List<AssetDependency> insertDependencies = new();
        
        foreach (var assetFile in assetFiles)
        {
            ++assetCommitCount;
            progressContext.Increment(1);
            try
            {
                if (string.IsNullOrEmpty(assetFile.Guid))
                    continue;
                var insertObj = new AssetFile()
                {
                    Guid = assetFile.Guid,
                    DirId = assetFile.DirNum,
                    Filename = assetFile.Filename,
                    DependencyCount = assetFile.DependencyCount
                };
                insertAssetFiles.Add(insertObj);
                
                insertDependencies.Clear();
                if (assetFile.DependencyCount > 0)
                {
                    string dependencyQuery = MakeDependencyQuery(assetFile.Guid, assetFile.Dependencies);
                    _db.Execute(dependencyQuery);
                }
                // 주기적으로 커밋
                if (assetCommitCount % 100 == 0)
                {
                    string assetQuery1 = MakeAssetFileQuery(insertAssetFiles);
                    _db.Execute(assetQuery1);
                    insertAssetFiles.Clear();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add asset in bulk");
            }
        }

        // 마지막 남은 데이터 커밋
        string assetQuery2 = MakeAssetFileQuery(insertAssetFiles);
        _db.Execute(assetQuery2);
        await Task.CompletedTask;
    }
}
