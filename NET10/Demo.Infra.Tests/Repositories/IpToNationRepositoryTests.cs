using Demo.Infra.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Bogus;

namespace Demo.Infra.Tests.Repositories;

public class IpToNationRepositoryTests : IDisposable
{
    private readonly Mock<ILogger<IpToNationRepository>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockHostingEnvironment;
    private readonly Faker _faker;
    private readonly string _testDbPath;

    public IpToNationRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<IpToNationRepository>>();
        _mockHostingEnvironment = new Mock<IWebHostEnvironment>();
        _faker = new Faker();
        
        // 실제 IP2LOCATION-LITE-DB3.BIN 파일이 있는 테스트 프로젝트 경로 설정
        var testProjectPath = GetTestProjectPath();
        _testDbPath = Path.Combine(testProjectPath, "IP2LOCATION-LITE-DB3.BIN");
        
        // Mock 설정 - 실제 파일이 있는 경로로 설정
        _mockHostingEnvironment
            .Setup(env => env.ContentRootPath)
            .Returns(testProjectPath);
    }

    private string GetTestProjectPath()
    {
        // 현재 실행 중인 어셈블리 위치에서 테스트 프로젝트 루트 경로 찾기
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);
        
        // bin/Debug/net9.0에서 프로젝트 루트로 이동
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "IP2LOCATION-LITE-DB3.BIN")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? throw new DirectoryNotFoundException("테스트 프로젝트 경로를 찾을 수 없습니다.");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Database_File_Not_Found()
    {
        // Arrange
        var nonExistentPath = "/nonexistent/path";
        _mockHostingEnvironment
            .Setup(env => env.ContentRootPath)
            .Returns(nonExistentPath);

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object));
        
        exception.Should().NotBeNull();
        
        // 로그 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_Should_Log_Error_And_Throw_When_File_Access_Denied()
    {
        // Arrange
        var restrictedPath = "/root"; // 접근 권한이 없는 경로
        _mockHostingEnvironment
            .Setup(env => env.ContentRootPath)
            .Returns(restrictedPath);

        // Act & Assert
        Exception? exception = null;
        try
        {
            new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        exception.Should().NotBeNull();
        
        // 로거가 호출되었는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    public async Task GetAsync_Should_Return_Country_Code_For_Valid_Private_IPs(string ipAddress)
    {
        // Arrange - 실제 IP2LOCATION-LITE-DB3.BIN 파일 사용
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }
        
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);

        // Act
        var result = await repository.GetAsync(ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // Private IP addresses typically return "unknown" or specific codes
        result.Should().BeOneOf("unknown", "-", "XX", "ZZ", "AP", "EU");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("256.256.256.256")]
    public async Task GetAsync_Should_Return_Unknown_For_Invalid_IP_Addresses(string invalidIp)
    {
        // Arrange - 실제 IP2LOCATION-LITE-DB3.BIN 파일 사용
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }
        
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);

        // Act
        var result = await repository.GetAsync(invalidIp);

        // Assert
        result.Should().NotBeNull();
        // Invalid IPs typically return "unknown" or error status
        result.Should().BeOneOf("unknown", "-", "XX", "ZZ");
    }

    [Fact]
    public async Task GetAsync_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange - 실제 IP2LOCATION-LITE-DB3.BIN 파일 사용
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }
        
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);
        var ipAddresses = new[]
        {
            "127.0.0.1",
            "192.168.1.1",
            "10.0.0.1",
            "172.16.0.1",
            "169.254.1.1"
        };

        // Act
        var tasks = ipAddresses.Select(ip => repository.GetAsync(ip)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task GetAsync_Should_Return_Consistent_Results_For_Same_IP()
    {
        // Arrange - 실제 IP2LOCATION-LITE-DB3.BIN 파일 사용
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }
        
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);
        var ipAddress = "192.168.1.100";

        // Act
        var result1 = await repository.GetAsync(ipAddress);
        var result2 = await repository.GetAsync(ipAddress);
        var result3 = await repository.GetAsync(ipAddress);

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
        result1.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_Should_Handle_Null_IP_Gracefully()
    {
        // Arrange - 실제 IP2LOCATION-LITE-DB3.BIN 파일 사용
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }
        
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);

        // Act
        var result = await repository.GetAsync(null!);

        // Assert
        result.Should().NotBeNull();
        // Null IP should return unknown or error status
        result.Should().BeOneOf("unknown", "-", "XX", "ZZ");
    }

    [Fact]
    public async Task GetAsync_Should_Be_Thread_Safe()
    {
        // Arrange - 실제 IP2LOCATION-LITE-DB3.BIN 파일 사용
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }
        
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);
        var ipAddress = "192.168.1.1";
        var taskCount = 100;

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => repository.GetAsync(ipAddress))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(taskCount);
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        });

        // 모든 결과가 동일해야 함
        var firstResult = results.First();
        results.Should().AllSatisfy(result => result.Should().Be(firstResult));
    }


    public void Dispose()
    {
        // 실제 파일을 사용하므로 정리할 필요 없음
        // IP2LOCATION-LITE-DB3.BIN은 테스트 프로젝트의 고정 파일
    }
}

/// <summary>
/// 실제 IP2LOCATION-LITE-DB3.BIN 파일을 사용한 통합 테스트
/// </summary>
public class IpToNationRepositoryIntegrationTests
{
    private readonly Mock<ILogger<IpToNationRepository>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockHostingEnvironment;
    private readonly string _testDbPath;

    public IpToNationRepositoryIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<IpToNationRepository>>();
        _mockHostingEnvironment = new Mock<IWebHostEnvironment>();
        
        // 실제 IP2LOCATION-LITE-DB3.BIN 파일이 있는 테스트 프로젝트 경로 설정
        var testProjectPath = GetTestProjectPath();
        _testDbPath = Path.Combine(testProjectPath, "IP2LOCATION-LITE-DB3.BIN");
        
        // Mock 설정 - 실제 파일이 있는 경로로 설정
        _mockHostingEnvironment
            .Setup(env => env.ContentRootPath)
            .Returns(testProjectPath);
    }

    private string GetTestProjectPath()
    {
        // 현재 실행 중인 어셈블리 위치에서 테스트 프로젝트 루트 경로 찾기
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);
        
        // bin/Debug/net9.0에서 프로젝트 루트로 이동
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "IP2LOCATION-LITE-DB3.BIN")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? throw new DirectoryNotFoundException("테스트 프로젝트 경로를 찾을 수 없습니다.");
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Path()
    {
        // Arrange - 실제 파일 존재 확인
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }

        // Act & Assert
        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);
        repository.Should().NotBeNull();
    }

    [Fact]
    public async Task Repository_Should_Handle_High_Load()
    {
        // Arrange - 실제 파일 존재 확인
        if (!File.Exists(_testDbPath))
        {
            Assert.Fail($"IP2LOCATION-LITE-DB3.BIN 파일을 찾을 수 없습니다: {_testDbPath}");
        }

        var repository = new IpToNationRepository(_mockLogger.Object, _mockHostingEnvironment.Object);
        var ipAddresses = new[]
        {
            "8.8.8.8", "1.1.1.1", "127.0.0.1", "192.168.1.1", "10.0.0.1",
            "172.16.0.1", "169.254.1.1", "224.0.0.1", "255.255.255.255"
        };

        // Act
        var tasks = new List<Task<string>>();
        for (int i = 0; i < 50; i++) // 50회 반복
        {
            foreach (var ip in ipAddresses)
            {
                tasks.Add(repository.GetAsync(ip));
            }
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(450); // 9 IPs × 50 iterations
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        });

        // 450개 조회가 10초 이내에 완료되어야 함
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);
    }
}