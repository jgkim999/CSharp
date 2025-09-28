using Demo.Application.Utils;
using FluentAssertions;
using System.Net;
using System.Text.RegularExpressions;

namespace Demo.Application.Tests.Utils;

/// <summary>
/// FakeIpGenerator 클래스의 단위 테스트
/// 가짜 IP 주소 생성 유틸리티 테스트
/// </summary>
public class FakeIpGeneratorTests
{
    /// <summary>
    /// IP 주소 유효성을 검사하는 정규식
    /// </summary>
    private static readonly Regex IpAddressRegex = new(
        @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
        RegexOptions.Compiled);

    [Fact]
    public void Get_ShouldReturnValidIpAddress()
    {
        // Act
        var ip = FakeIpGenerator.Get();

        // Assert
        ip.Should().NotBeNull();
        ip.Should().NotBeEmpty();
        IsValidIpAddress(ip!).Should().BeTrue($"Generated IP '{ip}' should be a valid IPv4 address");
    }

    [Fact]
    public void Get_ShouldReturnPredefinedIpRanges()
    {
        // Arrange
        var generatedIps = new List<string>();
        var expectedPrefixes = new[]
        {
            "1.119.", // China
            "1.33.",  // Japan
            "1.36.",  // Hong Kong
            "100.128.", // USA
            "101.127.", // Singapore
            "1.32."   // Korea
        };

        // Act - Generate many IPs to ensure we get all possible ranges
        for (int i = 0; i < 1000; i++)
        {
            var ip = FakeIpGenerator.Get();
            if (ip != null)
            {
                generatedIps.Add(ip);
            }
        }

        // Assert
        generatedIps.Should().NotBeEmpty();

        // Check that we got IPs from all expected ranges
        foreach (var prefix in expectedPrefixes)
        {
            generatedIps.Should().Contain(ip => ip.StartsWith(prefix),
                $"Should generate IPs with prefix '{prefix}'");
        }
    }

    [Theory]
    [InlineData("1.119.")]  // China
    [InlineData("1.33.")]   // Japan
    [InlineData("1.36.")]   // Hong Kong
    [InlineData("100.128.")] // USA
    [InlineData("101.127.")] // Singapore
    [InlineData("1.32.")]   // Korea
    public void Get_ShouldGenerateIpsFromExpectedRanges(string expectedPrefix)
    {
        // Arrange
        var attempts = 0;
        var maxAttempts = 100;
        var found = false;

        // Act
        while (attempts < maxAttempts && !found)
        {
            var ip = FakeIpGenerator.Get();
            if (ip != null && ip.StartsWith(expectedPrefix))
            {
                found = true;
            }
            attempts++;
        }

        // Assert
        found.Should().BeTrue($"Should generate at least one IP with prefix '{expectedPrefix}' within {maxAttempts} attempts");
    }

    [Fact]
    public void Get_CalledMultipleTimes_ShouldReturnDifferentIps()
    {
        // Arrange
        var generatedIps = new HashSet<string>();
        const int numberOfCalls = 50;

        // Act
        for (int i = 0; i < numberOfCalls; i++)
        {
            var ip = FakeIpGenerator.Get();
            if (ip != null)
            {
                generatedIps.Add(ip);
            }
        }

        // Assert
        generatedIps.Should().HaveCountGreaterThan(1, "Should generate different IP addresses");
    }

    [Fact]
    public void Get_ShouldGenerateValidLastOctet()
    {
        // Arrange
        const int numberOfTests = 100;

        // Act & Assert
        for (int i = 0; i < numberOfTests; i++)
        {
            var ip = FakeIpGenerator.Get();
            ip.Should().NotBeNull();

            var parts = ip!.Split('.');
            parts.Should().HaveCount(4, "IP should have 4 octets");

            // Check last octet is valid (1-255 for most ranges, 1-255 for Singapore/Korea)
            var lastOctet = int.Parse(parts[3]);
            lastOctet.Should().BeInRange(0, 255, "Last octet should be valid");
        }
    }

    [Fact]
    public void Get_ShouldGenerateValidThirdOctet()
    {
        // Arrange
        const int numberOfTests = 100;

        // Act & Assert
        for (int i = 0; i < numberOfTests; i++)
        {
            var ip = FakeIpGenerator.Get();
            ip.Should().NotBeNull();

            var parts = ip!.Split('.');
            parts.Should().HaveCount(4, "IP should have 4 octets");

            var thirdOctet = int.Parse(parts[2]);

            // Check third octet based on the prefix
            if (ip.StartsWith("101.127.") || ip.StartsWith("1.32."))
            {
                // Singapore and Korea ranges have specific third octet range
                thirdOctet.Should().BeInRange(216, 217, "Third octet should be in range 216-217 for Singapore/Korea");
            }
            else
            {
                // Other ranges use 0-255
                thirdOctet.Should().BeInRange(0, 255, "Third octet should be valid");
            }
        }
    }

    [Fact]
    public void Get_ShouldUseRandomDistribution()
    {
        // Arrange
        var ipCounts = new Dictionary<string, int>();
        const int numberOfTests = 1000;

        // Act
        for (int i = 0; i < numberOfTests; i++)
        {
            var ip = FakeIpGenerator.Get();
            if (ip != null)
            {
                var prefix = ip.Substring(0, ip.LastIndexOf('.'));
                ipCounts[prefix] = ipCounts.GetValueOrDefault(prefix, 0) + 1;
            }
        }

        // Assert
        ipCounts.Should().NotBeEmpty();
        ipCounts.Keys.Should().HaveCountGreaterThan(1, "Should generate IPs from multiple ranges");

        // Each range should be represented (with some statistical variation)
        foreach (var count in ipCounts.Values)
        {
            count.Should().BeGreaterThan(0, "Each range should be used");
        }
    }

    [Fact]
    public void Get_ShouldNotReturnNull()
    {
        // Arrange & Act
        const int numberOfTests = 100;

        // Assert
        for (int i = 0; i < numberOfTests; i++)
        {
            var ip = FakeIpGenerator.Get();
            ip.Should().NotBeNull("FakeIpGenerator.Get() should never return null");
        }
    }

    [Fact]
    public void Get_ShouldNotReturnEmptyString()
    {
        // Arrange & Act
        const int numberOfTests = 100;

        // Assert
        for (int i = 0; i < numberOfTests; i++)
        {
            var ip = FakeIpGenerator.Get();
            ip.Should().NotBeNullOrEmpty("FakeIpGenerator.Get() should never return empty string");
        }
    }

    [Fact]
    public void Get_GeneratedIps_ShouldBeParseableByIPAddress()
    {
        // Arrange
        const int numberOfTests = 50;

        // Act & Assert
        for (int i = 0; i < numberOfTests; i++)
        {
            var ip = FakeIpGenerator.Get();
            ip.Should().NotBeNull();

            var isValidIp = IPAddress.TryParse(ip, out var parsedIp);
            isValidIp.Should().BeTrue($"Generated IP '{ip}' should be parseable by IPAddress.TryParse");
            parsedIp.Should().NotBeNull();
            parsedIp!.AddressFamily.Should().Be(System.Net.Sockets.AddressFamily.InterNetwork,
                "Generated IP should be IPv4");
        }
    }

    [Fact]
    public void Get_ShouldGenerateKoreaIpRange()
    {
        // Act & Assert
        var found = false;
        for (int i = 0; i < 100 && !found; i++)
        {
            var ip = FakeIpGenerator.Get();
            if (ip != null && ip.StartsWith("1.32."))
            {
                found = true;
                // Korea range should have third octet in 216-217
                var parts = ip.Split('.');
                var thirdOctet = int.Parse(parts[2]);
                thirdOctet.Should().BeInRange(216, 217);
            }
        }

        found.Should().BeTrue("Should generate at least one Korea IP range");
    }

    [Fact]
    public async Task Get_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        const int numberOfThreads = 10;
        const int callsPerThread = 100;
        var tasks = new Task[numberOfThreads];
        var allIps = new List<string>[numberOfThreads];

        // Act
        for (int i = 0; i < numberOfThreads; i++)
        {
            var threadIndex = i;
            allIps[threadIndex] = new List<string>();

            tasks[threadIndex] = Task.Run(() =>
            {
                for (int j = 0; j < callsPerThread; j++)
                {
                    var ip = FakeIpGenerator.Get();
                    if (ip != null)
                    {
                        allIps[threadIndex].Add(ip);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var threadIps in allIps)
        {
            threadIps.Should().HaveCount(callsPerThread, "Each thread should generate the expected number of IPs");
            threadIps.Should().OnlyContain(ip => IsValidIpAddress(ip), "All generated IPs should be valid");
        }
    }

    /// <summary>
    /// IP 주소가 유효한지 검사하는 헬퍼 메서드
    /// </summary>
    private static bool IsValidIpAddress(string ip)
    {
        return IpAddressRegex.IsMatch(ip) && IPAddress.TryParse(ip, out _);
    }
}