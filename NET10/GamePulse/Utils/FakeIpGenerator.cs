namespace GamePulse.Utils;

/// <summary>
/// Utility class for generating fake IP addresses
/// </summary>
public static class FakeIpGenerator
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a fake IPv4 address string
    /// </summary>
    /// <returns>A fake IPv4 address in string format</returns>
    public static string? Get()
    {
        //return $"{Random.Next(1, 255)}.{Random.Next(0, 255)}.{Random.Next(0, 255)}.{Random.Next(1, 255)}";
        var c = Random.Next(0, 6);
        return c switch
        {
            0 => $"1.119.{Random.Next(0, 255)}.{Random.Next(0, 255)}", // China
            1 => $"1.33.{Random.Next(0, 255)}.{Random.Next(0, 255)}", // Japan
            2 => $"1.36.{Random.Next(0, 255)}.{Random.Next(0, 255)}", // HongKong
            3 => $"100.128.{Random.Next(0, 255)}.{Random.Next(0, 255)}", // USA
            4 => $"101.127.{Random.Next(216, 217)}.{Random.Next(1, 255)}", // Singapore
            _ => $"1.32.{Random.Next(216, 217)}.{Random.Next(1, 255)}" // Korea
        };
    }
}
