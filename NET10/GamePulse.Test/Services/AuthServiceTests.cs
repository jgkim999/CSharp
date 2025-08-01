using FluentAssertions;
using OpenTelemetry.Trace;
using OpenTelemetry;
using GamePulse.Services.Auth;
using System.Diagnostics;

namespace GamePulse.Test.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AuthService _authService;
    private readonly TracerProvider _tracerProvider;
    private readonly Tracer _tracer;

    public AuthServiceTests()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder().Build();
        _tracer = _tracerProvider.GetTracer("test");
        _authService = new AuthService(_tracer);
    }

    [Fact]
    public async Task CredentialsAreValidAsync_ValidPassword_ReturnsTrue()
    {
        // Arrange
        var username = "testuser";
        var password = "admin";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "admin")]
    [InlineData(null, "admin")]
    [InlineData("testuser", "")]
    [InlineData("testuser", null)]
    [InlineData("", "")]
    [InlineData(null, null)]
    public async Task CredentialsAreValidAsync_NullOrEmptyCredentials_OnlyValidWhenPasswordIsAdmin(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        if (password == "admin")
            result.Should().BeTrue();
        else
            result.Should().BeFalse();
    }

    [Theory]
    [InlineData("testuser", "ADMIN")]
    [InlineData("testuser", "Admin")]
    [InlineData("testuser", "AdMiN")]
    [InlineData("TESTUSER", "admin")]
    [InlineData("TestUser", "admin")]
    public async Task CredentialsAreValidAsync_CaseSensitivePassword_OnlyExactAdminMatches(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        if (password == "admin")
            result.Should().BeTrue();
        else
            result.Should().BeFalse();
    }

    [Theory]
    [InlineData("admin", "admin")]
    [InlineData("user", "admin")]
    [InlineData("guest", "admin")]
    [InlineData("administrator", "admin")]
    [InlineData("", "admin")]
    [InlineData(null, "admin")]
    public async Task CredentialsAreValidAsync_DifferentUsernames_WithValidPassword_ReturnsTrue(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("testuser", "password")]
    [InlineData("testuser", "123456")]
    [InlineData("testuser", "test")]
    [InlineData("testuser", "wrongpass")]
    [InlineData("testuser", "invalid")]
    [InlineData("testuser", "administrator")]
    [InlineData("testuser", "admins")]
    [InlineData("testuser", "admi")]
    public async Task CredentialsAreValidAsync_ValidUsername_InvalidPasswords_ReturnsFalse(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var username = "testuser";
        var password = "admin";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cts.Token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithAlreadyCancelledToken_StillCompletes()
    {
        // Arrange - Since the method doesn't actually use the cancellation token,
        // it should complete even with a cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var username = "testuser";
        var password = "admin";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cts.Token);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("user@domain.com", "admin")]
    [InlineData("user.name", "admin")]
    [InlineData("user_name", "admin")]
    [InlineData("user-name", "admin")]
    [InlineData("123user", "admin")]
    [InlineData("user with spaces", "admin")]
    public async Task CredentialsAreValidAsync_SpecialCharactersInUsername_WithValidPassword_ReturnsTrue(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("testuser", "admin123")]
    [InlineData("testuser", "admin@123")]
    [InlineData("testuser", "admin_password")]
    [InlineData("testuser", "admin-password")]
    [InlineData("testuser", "admin ")]
    [InlineData("testuser", " admin")]
    [InlineData("testuser", " admin ")]
    public async Task CredentialsAreValidAsync_ModifiedAdminPassword_ReturnsFalse(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("a", "admin")]
    [InlineData("ab", "admin")]
    [InlineData("verylongusernamethatexceedsnormallimits", "admin")]
    public async Task CredentialsAreValidAsync_VariousUsernameLengths_WithValidPassword_ReturnsTrue(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("testuser", "admin\n")]
    [InlineData("testuser", "admin\r")]
    [InlineData("testuser", "admin\t")]
    [InlineData("testuser", "admin\0")]
    public async Task CredentialsAreValidAsync_ControlCharactersInPassword_ReturnsFalse(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("testuser", "ädmin")]
    [InlineData("testuser", "admïn")]
    [InlineData("testuser", "админ")]
    [InlineData("tëstuser", "admin")]
    [InlineData("测试用户", "admin")]
    [InlineData("🚀", "admin")]
    public async Task CredentialsAreValidAsync_UnicodeCharacters_HandledCorrectly(string username, string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        if (password == "admin")
            result.Should().BeTrue();
        else
            result.Should().BeFalse();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_ConcurrentCalls_AllComplete()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            var username = $"user{i}";
            var password = i % 2 == 0 ? "admin" : "wrong";
            tasks.Add(_authService.CredentialsAreValidAsync(username, password, CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        for (int i = 0; i < results.Length; i++)
        {
            if (i % 2 == 0)
                results[i].Should().BeTrue($"user{i} with 'admin' password should be valid");
            else
                results[i].Should().BeFalse($"user{i} with 'wrong' password should be invalid");
        }
    }

    [Fact]
    public async Task CredentialsAreValidAsync_MultipleCallsSameCredentials_ConsistentResults()
    {
        // Arrange
        var username = "testuser";
        var password = "admin";

        // Act
        var result1 = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);
        var result2 = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);
        var result3 = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
        new[] { result1, result2, result3 }.Should().AllSatisfy(r => r.Should().BeTrue());
    }

    [Fact]
    public void Constructor_WithValidTracer_CreatesInstance()
    {
        // Arrange
        var tracerProvider = Sdk.CreateTracerProviderBuilder().Build();
        var tracer = tracerProvider.GetTracer("test");

        // Act
        var authService = new AuthService(tracer);

        // Assert
        authService.Should().NotBeNull();
        authService.Should().BeOfType<AuthService>();
        tracerProvider.Dispose();
    }

    [Fact]
    public void Constructor_WithNullTracer_CreatesInstanceSuccessfully()
    {
        // Act
        var authService = new AuthService(null);

        // Assert
        authService.Should().NotBeNull();
        authService.Should().BeOfType<AuthService>();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithNullTracer_WorksCorrectly()
    {
        // Arrange
        var authServiceWithNullTracer = new AuthService(null);
        var username = "testuser";
        var password = "admin";

        // Act
        var result = await authServiceWithNullTracer.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_PerformanceTest_CompletesInReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var username = "testuser";
        var password = "admin";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        stopwatch.Stop();
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should complete very quickly
    }

    [Fact]
    public async Task CredentialsAreValidAsync_MassiveParallelCalls_HandlesLoad()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        const int numberOfCalls = 1000;

        // Act
        for (int i = 0; i < numberOfCalls; i++)
        {
            var password = i % 3 == 0 ? "admin" : "invalid";
            tasks.Add(_authService.CredentialsAreValidAsync($"user{i}", password, CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(numberOfCalls);
        var validResults = results.Where((_, index) => index % 3 == 0);
        var invalidResults = results.Where((_, index) => index % 3 != 0);
        
        validResults.Should().AllSatisfy(r => r.Should().BeTrue());
        invalidResults.Should().AllSatisfy(r => r.Should().BeFalse());
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ad")]
    [InlineData("adm")]
    [InlineData("admi")]
    [InlineData("admins")]
    [InlineData("admin1")]
    [InlineData("1admin")]
    public async Task CredentialsAreValidAsync_PasswordSimilarToAdmin_OnlyExactAdminReturnsTrue(string password)
    {
        // Act
        var result = await _authService.CredentialsAreValidAsync("testuser", password, CancellationToken.None);

        // Assert
        if (password == "admin")
            result.Should().BeTrue();
        else
            result.Should().BeFalse();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_EmptyStringVsNull_BehaviorConsistent()
    {
        // Act
        var resultWithEmptyUsername = await _authService.CredentialsAreValidAsync("", "admin", CancellationToken.None);
        var resultWithNullUsername = await _authService.CredentialsAreValidAsync(null, "admin", CancellationToken.None);
        var resultWithEmptyPassword = await _authService.CredentialsAreValidAsync("user", "", CancellationToken.None);
        var resultWithNullPassword = await _authService.CredentialsAreValidAsync("user", null, CancellationToken.None);

        // Assert
        resultWithEmptyUsername.Should().BeTrue();
        resultWithNullUsername.Should().BeTrue();
        resultWithEmptyPassword.Should().BeFalse();
        resultWithNullPassword.Should().BeFalse();
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
    }
}