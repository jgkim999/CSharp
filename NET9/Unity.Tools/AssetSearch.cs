﻿using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Unity.Tools.Models;

using YamlDotNet.RepresentationModel;

namespace Unity.Tools;

public class AssetSearch
{
    static string[] MetaExtensions = new string[] { ".meta" };

    static string YamlPattern = @"\{(.*?)\}";
    private static Regex YamlRegex = new(YamlPattern);

    static string GuidPattern = @"guid:\s*([a-f0-9]{32})";
    private static Regex GuidRegex = new(GuidPattern);

    public static async Task<List<string>> DirectorySearchAsync(
        string? root,
        ILogger logger,
        string[] ignoreDirectoryNames)
    {
        List<string> directories = new();
        // Count of files traversed and timer for diagnostic output
        var sw = Stopwatch.StartNew();

        // Determine whether to parallelize file processing on each folder based on processor count.
        int procCount = Environment.ProcessorCount;

        // Data structure to hold names of subfolders to be examined for files.
        Stack<string?> dirs = new Stack<string?>();

        if (!Directory.Exists(root))
        {
            logger.LogError("Directory {Root} does not exist.", root);
            return directories;
        }

        dirs.Push(root);
        directories.Add(root);

        await Task.Run(() =>
        {
            while (dirs.Count > 0)
            {
                string? currentDir = dirs.Pop();
                string?[] subDirs;

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                // Thrown if we do not have discovery permission on the directory.
                catch (UnauthorizedAccessException e)
                {
                    logger.LogError(e, "Directory.GetDirectories UnauthorizedAccessException: {CurrentDir}",
                        currentDir);
                    continue;
                }
                // Thrown if another process has deleted the directory after we retrieved its name.
                catch (DirectoryNotFoundException e)
                {
                    logger.LogError(e, "DirectoryNotFoundException: {CurrentDir}", currentDir);
                    continue;
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string? subDir in subDirs)
                {
                    if (ignoreDirectoryNames.Contains(Path.GetFileName(subDir)))
                    {
                        continue;
                    }

                    dirs.Push(subDir);
                    directories.Add(subDir);
                    //logger.LogInformation(subDir);
                }
            }
        });

        // For diagnostic purposes.
        logger.LogInformation(
            "Processed {DirectoriesCount} directories in {ElapsedMilliseconds} milliseconds",
            directories.Count, sw.ElapsedMilliseconds);
        return directories;
    }

    public static async Task<ConcurrentBag<UnityMetaFileInfo>> MakeMetaListAsync(
        Dictionary<int, string> directories,
        ILogger logger,
        IProgressContext progressContext)
    {
        ConcurrentBag<UnityMetaFileInfo> metaFiles = new();
        progressContext.StartTask();
        progressContext.SetMaxValue(directories.Count);
        var sw = Stopwatch.StartNew();

        await Task.Run(() =>
        {
            Parallel.ForEach(directories, directory =>
            {
                progressContext.Increment(1);

                //logger.LogInformation(directory.Value);
                var files = Directory.GetFiles(directory.Value);
                foreach (var file in files)
                {
                    if (Path.HasExtension(file) == false)
                    {
                        continue;
                    }

                    var ext = Path.GetExtension(file);
                    if (MetaExtensions.Contains(ext) == false)
                    {
                        continue;
                    }

                    metaFiles.Add(new UnityMetaFileInfo()
                    {
                        DirNum = directory.Key, MetaFilename = Path.GetFileName(file)
                    });
                }
            });
        });
        logger.LogInformation(
            "Processed {MetaFilesCount} files in {ElapsedMilliseconds} milliseconds",
            metaFiles.Count,
            sw.ElapsedMilliseconds);
        progressContext.StopTask();
        return metaFiles;
    }

    public static async Task<ConcurrentBag<UnityCacheFileInfo>> MakeBuildCacheFileListAsync(
        Dictionary<int, string> directories,
        ILogger logger,
        IProgressContext progressContext)
    {
        ConcurrentBag<UnityCacheFileInfo> buildCacheFiles = new();
        progressContext.StartTask();
        progressContext.SetMaxValue(directories.Count);
        var sw = Stopwatch.StartNew();

        await Task.Run(() =>
        {
            Parallel.ForEach(directories, directory =>
            {
                progressContext.Increment(1);

                //logger.LogInformation(directory.Value);
                var files = Directory.GetFiles(directory.Value);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);

                    buildCacheFiles.Add(new UnityCacheFileInfo()
                    {
                        DirNum = directory.Key,
                        Filename = Path.GetFileName(file),
                        Length = fileInfo.Length,
                        CreationTimeUtc = fileInfo.CreationTimeUtc,
                        LastWriteTimeUtc = fileInfo.LastAccessTimeUtc,
                        LastAccessTimeUtc = fileInfo.LastWriteTimeUtc
                    });
                }
            });
        });
        logger.LogInformation(
            "Processed {BuildCacheFilesCount} files in {ElapsedMilliseconds} milliseconds",
            buildCacheFiles.Count,
            sw.ElapsedMilliseconds);
        progressContext.StopTask();
        return buildCacheFiles;
    }

    public static async Task MakeMetaGuidAsync(
        Dictionary<int, string> directories,
        ConcurrentBag<UnityMetaFileInfo> metaFiles,
        ILogger logger,
        IProgressContext progressContext,
        CancellationToken cancellationToken = default)
    {
        progressContext.SetMaxValue(metaFiles.Count);
        progressContext.StartTask();

        await Task.Run(() =>
        {
            Parallel.ForEach(metaFiles, (metaFile, cancelToken) =>
            {
                progressContext.Increment(1f);
                string? dir;
                if (directories.TryGetValue(metaFile.DirNum, out dir))
                {
                    var filePath = Path.Combine(dir, metaFile.MetaFilename);
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var yamlContent = File.ReadAllText(filePath);
                            var yaml = new YamlStream();
                            yaml.Load(new StringReader(yamlContent));

                            // YAML 문서의 루트 노드를 가져옵니다.
                            var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

                            // GUID 값을 가져옵니다.
                            if (rootNode.Children.TryGetValue(new YamlScalarNode("guid"), out var guidNode))
                            {
                                metaFile.Guid = guidNode.ToString();
                            }
                            else
                            {
                                logger.LogError("{FilePath} GUID를 찾을 수 없습니다.", filePath);
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "{FilePath}", filePath);
                        }
                    }
                }
            });
        });
        progressContext.StopTask();
    }

    public static async Task MakeDependencyAsync(
        Dictionary<int, string> directories,
        ConcurrentBag<UnityMetaFileInfo> metaFiles,
        string[] extNames,
        string[] ignoreGuids,
        ILogger logger,
        IProgressContext progressContext,
        CancellationToken cancellationToken = default)
    {
        progressContext.SetMaxValue(metaFiles.Count);
        progressContext.StartTask();
        await Task.Run(() =>
        {
            Parallel.ForEach(metaFiles, (metafile, cancelToken) =>
            {
                progressContext.Increment(1);
                if (extNames.Contains(metafile.Extension) == false)
                    return;
                string dir;
                if (directories.TryGetValue(metafile.DirNum, out dir) == false)
                {
                    logger.LogError(
                        "Unknown directory: {DirNum} {MetaFilename}",
                        metafile.DirNum,
                        metafile.MetaFilename);
                    return;
                }

                var filePath = Path.Combine(dir, metafile.Filename);
                if (File.Exists(filePath) == false)
                {
                    logger.LogError("File {FilePath} does not exist.", filePath);
                    return;
                }

                // 파일의 각 줄을 비동기적으로 읽습니다.
                foreach (var line in File.ReadLines(filePath))
                {
                    if (line.Contains("guid") == false)
                        continue;
                    // 매칭 결과
                    Match match1 = YamlRegex.Match(line);
                    if (match1.Success)
                    {
                        // 중괄호 내부의 문자열 추출
                        string result = match1.Groups[1].Value;
                        //logger.LogInformation($"{filePath} {result}");

                        Match match2 = GuidRegex.Match(line);
                        if (match2.Success)
                        {
                            string result2 = match2.Groups[1].Value;
                            if (ignoreGuids.Contains(result2))
                                continue;
                            metafile.Dependencies.Add(result2);
                            //logger.LogInformation($"{filePath} {result2}");   
                        }
                    }
                }

                if (metafile.Dependencies.Count > 0)
                    logger.LogInformation(
                        "{FilePath} Dependencies: {DependencyCount}",
                        filePath, metafile.Dependencies.Count);
            });
        });
        progressContext.StopTask();
    }
}