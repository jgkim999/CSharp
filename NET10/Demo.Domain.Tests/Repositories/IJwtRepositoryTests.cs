using Demo.Domain.Repositories;
using FluentAssertions;
using Moq;
using Bogus;

namespace Demo.Domain.Tests.Repositories;

public class IJwtRepositoryTests
{
    private readonly Mock<IJwtRepository> _mockRepository;
    private readonly Faker _faker;

    public IJwtRepositoryTests()
    {
        _mockRepository = new Mock<IJwtRepository>();
        _faker = new Faker();
    }

    [Fact]
    public async Task StoreTokenAsync_Should_Store_Token_Successfully()
    {
        // Arrange
        var userId = _faker.Random.Guid().ToString();
        var refreshToken = _faker.Random.Hash();

        _mockRepository
            .Setup(r => r.StoreTokenAsync(userId, refreshToken))
            .Returns(Task.CompletedTask);

        // Act
        await _mockRepository.Object.StoreTokenAsync(userId, refreshToken);

        // Assert
        _mockRepository.Verify(
            r => r.StoreTokenAsync(userId, refreshToken),
            Times.Once);
    }

    [Theory]
    [InlineData("user123", "token123", true)]
    [InlineData("user456", "token456", true)]
    [InlineData("user789", "invalidtoken", false)]
    public async Task TokenIsValidAsync_Should_Return_Expected_Result(string userId, string refreshToken, bool expectedResult)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.TokenIsValidAsync(userId, refreshToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockRepository.Object.TokenIsValidAsync(userId, refreshToken);

        // Assert
        result.Should().Be(expectedResult);

        _mockRepository.Verify(
            r => r.TokenIsValidAsync(userId, refreshToken),
            Times.Once);
    }

    [Fact]
    public async Task Complete_Token_Workflow_Should_Work_Correctly()
    {
        // Arrange
        var userId = _faker.Random.Guid().ToString();
        var refreshToken = _faker.Random.Hash();

        _mockRepository
            .Setup(r => r.StoreTokenAsync(userId, refreshToken))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.TokenIsValidAsync(userId, refreshToken))
            .ReturnsAsync(true);

        // Act
        await _mockRepository.Object.StoreTokenAsync(userId, refreshToken);
        var isValid = await _mockRepository.Object.TokenIsValidAsync(userId, refreshToken);

        // Assert
        isValid.Should().BeTrue();

        _mockRepository.Verify(
            r => r.StoreTokenAsync(userId, refreshToken),
            Times.Once);
        _mockRepository.Verify(
            r => r.TokenIsValidAsync(userId, refreshToken),
            Times.Once);
    }

    [Fact]
    public async Task TokenIsValidAsync_Should_Return_False_For_Non_Existent_User()
    {
        // Arrange
        var nonExistentUserId = _faker.Random.Guid().ToString();
        var refreshToken = _faker.Random.Hash();

        _mockRepository
            .Setup(r => r.TokenIsValidAsync(nonExistentUserId, refreshToken))
            .ReturnsAsync(false);

        // Act
        var result = await _mockRepository.Object.TokenIsValidAsync(nonExistentUserId, refreshToken);

        // Assert
        result.Should().BeFalse();

        _mockRepository.Verify(
            r => r.TokenIsValidAsync(nonExistentUserId, refreshToken),
            Times.Once);
    }

    [Fact]
    public async Task TokenIsValidAsync_Should_Return_False_For_Wrong_Token()
    {
        // Arrange
        var userId = _faker.Random.Guid().ToString();
        var correctToken = _faker.Random.Hash();
        var wrongToken = _faker.Random.Hash();

        // Setup: Store correct token
        _mockRepository
            .Setup(r => r.StoreTokenAsync(userId, correctToken))
            .Returns(Task.CompletedTask);

        // Setup: Wrong token should return false
        _mockRepository
            .Setup(r => r.TokenIsValidAsync(userId, wrongToken))
            .ReturnsAsync(false);

        // Setup: Correct token should return true
        _mockRepository
            .Setup(r => r.TokenIsValidAsync(userId, correctToken))
            .ReturnsAsync(true);

        // Act
        await _mockRepository.Object.StoreTokenAsync(userId, correctToken);
        var wrongTokenResult = await _mockRepository.Object.TokenIsValidAsync(userId, wrongToken);
        var correctTokenResult = await _mockRepository.Object.TokenIsValidAsync(userId, correctToken);

        // Assert
        wrongTokenResult.Should().BeFalse();
        correctTokenResult.Should().BeTrue();

        _mockRepository.Verify(
            r => r.StoreTokenAsync(userId, correctToken),
            Times.Once);
        _mockRepository.Verify(
            r => r.TokenIsValidAsync(userId, wrongToken),
            Times.Once);
        _mockRepository.Verify(
            r => r.TokenIsValidAsync(userId, correctToken),
            Times.Once);
    }

    [Fact]
    public async Task StoreTokenAsync_Should_Handle_Multiple_Concurrent_Operations()
    {
        // Arrange
        var userTokenPairs = GenerateUserTokenPairs(10);
        var tasks = new List<Task>();

        foreach (var (userId, token) in userTokenPairs)
        {
            _mockRepository
                .Setup(r => r.StoreTokenAsync(userId, token))
                .Returns(Task.CompletedTask);
        }

        // Act
        foreach (var (userId, token) in userTokenPairs)
        {
            tasks.Add(_mockRepository.Object.StoreTokenAsync(userId, token));
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var (userId, token) in userTokenPairs)
        {
            _mockRepository.Verify(
                r => r.StoreTokenAsync(userId, token),
                Times.Once);
        }
    }

    [Fact]
    public async Task StoreTokenAsync_Should_Handle_Exception()
    {
        // Arrange
        var userId = _faker.Random.Guid().ToString();
        var refreshToken = _faker.Random.Hash();

        _mockRepository
            .Setup(r => r.StoreTokenAsync(userId, refreshToken))
            .ThrowsAsync(new InvalidOperationException("Storage service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockRepository.Object.StoreTokenAsync(userId, refreshToken));

        _mockRepository.Verify(
            r => r.StoreTokenAsync(userId, refreshToken),
            Times.Once);
    }

    [Fact]
    public async Task TokenIsValidAsync_Should_Handle_Exception()
    {
        // Arrange
        var userId = _faker.Random.Guid().ToString();
        var refreshToken = _faker.Random.Hash();

        _mockRepository
            .Setup(r => r.TokenIsValidAsync(userId, refreshToken))
            .ThrowsAsync(new InvalidOperationException("Validation service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockRepository.Object.TokenIsValidAsync(userId, refreshToken));

        _mockRepository.Verify(
            r => r.TokenIsValidAsync(userId, refreshToken),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task StoreTokenAsync_Should_Handle_Empty_Or_Null_Values(string? value)
    {
        // Arrange
        var userId = value ?? string.Empty;
        var refreshToken = value ?? string.Empty;

        _mockRepository
            .Setup(r => r.StoreTokenAsync(userId, refreshToken))
            .Returns(Task.CompletedTask);

        // Act
        await _mockRepository.Object.StoreTokenAsync(userId, refreshToken);

        // Assert
        _mockRepository.Verify(
            r => r.StoreTokenAsync(userId, refreshToken),
            Times.Once);
    }

    private List<(string userId, string token)> GenerateUserTokenPairs(int count)
    {
        var pairs = new List<(string, string)>();
        for (int i = 0; i < count; i++)
        {
            pairs.Add((_faker.Random.Guid().ToString(), _faker.Random.Hash()));
        }
        return pairs;
    }
}