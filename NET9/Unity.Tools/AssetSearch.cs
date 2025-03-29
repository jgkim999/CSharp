using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

using YamlDotNet.RepresentationModel;

namespace Unity.Tools;

public class AssetSearch
{
    static string[] MetaExtensions = new string[] { ".meta" };
    static string[] IgnoreDirectories = new string[] { ".git", ".idea", ".github", ".vs", "bin" };

    public static void DirectorySearch(string root, List<string> directories, ILogger logger)
    {
        // Count of files traversed and timer for diagnostic output
        var sw = Stopwatch.StartNew();

        // Determine whether to parallelize file processing on each folder based on processor count.
        int procCount = Environment.ProcessorCount;

        // Data structure to hold names of subfolders to be examined for files.
        Stack<string> dirs = new Stack<string>();

        if (!Directory.Exists(root))
        {
            logger.LogError($"Directory {root} does not exist.");
            return;
        }

        dirs.Push(root);
        directories.Add(root);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs;

            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            // Thrown if we do not have discovery permission on the directory.
            catch (UnauthorizedAccessException e)
            {
                logger.LogError($"Directory.GetDirectories UnauthorizedAccessException: {currentDir}");
                logger.LogError(e.Message);
                continue;
            }
            // Thrown if another process has deleted the directory after we retrieved its name.
            catch (DirectoryNotFoundException e)
            {
                logger.LogError($"DirectoryNotFoundException: {currentDir}");
                logger.LogError(e.Message);
                continue;
            }

            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            foreach (string subDir in subDirs)
            {
                if (IgnoreDirectories.Contains(Path.GetFileName(subDir)))
                {
                    continue;
                }

                dirs.Push(subDir);
                directories.Add(subDir);
                //logger.LogInformation(subDir);
            }
        }

        // For diagnostic purposes.
        logger.LogInformation($"Processed {directories.Count} directories in {sw.ElapsedMilliseconds} milliseconds");
    }

    public static void MakeMetaList(
        Dictionary<int, string> directories,
        ConcurrentBag<UnityMetaFileInfo> metaFiles,
        ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        Parallel.ForEach(directories, directory =>
        {
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
                metaFiles.Add(new UnityMetaFileInfo() { DirNum = directory.Key, Filename = Path.GetFileName(file) });
            }
        });
        logger.LogInformation($"Processed {metaFiles.Count} files in {sw.ElapsedMilliseconds} milliseconds");
    }

    public async Task MetaYamlAsync(
        Dictionary<int, string> directories,
        ConcurrentBag<UnityMetaFileInfo> metaFiles,
        ILogger logger)
    {
        await Parallel.ForEachAsync(metaFiles, async (metaFile, cancellationToken) =>
        {
            if (directories.TryGetValue(metaFile.DirNum, out string dir))
            {
                var filePath = Path.Combine(dir, metaFile.Filename);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var yamlContent = await File.ReadAllTextAsync(filePath, cancellationToken);
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
                            logger.LogError($"GUID를 찾을 수 없습니다. {filePath}");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e.Message);
                    }
                }
            }
        });
    }
}