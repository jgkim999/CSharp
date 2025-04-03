using SpookilySharp;

using System.Security.Cryptography;

namespace Unity.Tools.Utils;

public class HashFunc
{
    public static string GetSpookyHash(string path)
    {
        var file = File.ReadAllBytes(path);
        
        SpookyHash sh = new SpookyHash();
        sh.Update(file);
        HashCode128 h = sh.Final();
        Console.WriteLine(h.ToString());
        return h.ToString();
    }

    public static string GetMd5Hash(string path)
    {
        using var md5 = MD5.Create();
        var file = File.ReadAllBytes(path);
        var hash = md5.ComputeHash(file);
        var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        Console.WriteLine(hashStr);
        return hashStr;
    }
}