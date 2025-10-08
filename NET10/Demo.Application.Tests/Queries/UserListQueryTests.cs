using Demo.Application.DTO.User;
using Demo.Application.Handlers.Queries;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Repositories;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using Mapster;

namespace Demo.Application.Tests.Queries;

/// <summary>
/// UserListQuery와 UserListQueryHandler의 단위 테스트
/// 사용자 목록 조회 쿼리 처리 로직 테스트
/// </summary>
public class UserListQueryTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserListQueryHandler>> _mockLogger;
    private readonly UserListQueryHandler _handler;
    private readonly Mock<ITelemetryService> _mockTelemetry;

    public UserListQueryTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserListQueryHandler>>();
        _mockTelemetry = new Mock<ITelemetryService>();
        _handler = new UserListQueryHandler(_mockRepository.Object, _mockTelemetry.Object, _mockLogger.Object);

        // Initialize Mapster configuration
        var config = new MapsterConfig();
        config.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task HandleAsync_WithValidQuery_ShouldReturnSuccessResult()
    {
        // Arrange
        const string searchTerm = "john";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>
        {
            new() { id = 1, name = "John Doe", email = "john@example.com", created_at = DateTime.UtcNow },
            new() { id = 2, name = "Jane Smith", email = "jane@example.com", created_at = DateTime.UtcNow }
        };
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 2));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalItems.Should().Be(2);
        result.Value.Items[0].Name.Should().Be("John Doe");
        result.Value.Items[1].Name.Should().Be("Jane Smith");

        _mockRepository.Verify(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullSearchTerm_ShouldPassNullToRepository()
    {
        // Arrange
        string? searchTerm = null;
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 0));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(x => x.GetPagedAsync(null, page, pageSize, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptySearchTerm_ShouldWork()
    {
        // Arrange
        const string searchTerm = "";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 0));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryFails_ShouldReturnFailureResult()
    {
        // Arrange
        const string searchTerm = "john";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var repositoryResult = Result.Fail<(IEnumerable<UserEntity> Users, int TotalCount)>("Database connection failed");

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Be("Database connection failed");

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("사용자 목록 조회 실패")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrowsException_ShouldReturnFailureResult()
    {
        // Arrange
        const string searchTerm = "john";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var expectedException = new InvalidOperationException("Database error");

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Contain("사용자 목록 조회 실패");
        result.Errors[0].Message.Should().Contain("Database error");

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("사용자 목록 조회 중 예외 발생")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        const string searchTerm = "nonexistent";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 0));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalItems.Should().Be(0);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 5)]
    [InlineData(10, 1)]
    [InlineData(1, 100)]
    public async Task HandleAsync_WithDifferentPagingParameters_ShouldPassCorrectValues(int page, int pageSize)
    {
        // Arrange
        const string searchTerm = "test";
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 0));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        await _handler.HandleAsync(query, cancellationToken);

        // Assert
        _mockRepository.Verify(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithLargeDataSet_ShouldHandleCorrectly()
    {
        // Arrange
        const string searchTerm = "user";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        // Create a large dataset
        var users = Enumerable.Range(1, pageSize)
            .Select(i => new UserEntity
            {
                id = i,
                name = $"User {i}",
                email = $"user{i}@example.com",
                created_at = DateTime.UtcNow
            })
            .ToList();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 1000)); // Total of 1000 users

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(pageSize);
        result.Value.TotalItems.Should().Be(1000);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapUserEntitiesToDtos()
    {
        // Arrange
        const string searchTerm = "test";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>
        {
            new() { id = 1, name = "John Doe", email = "john@example.com", created_at = DateTime.UtcNow },
            new() { id = 2, name = "Jane Smith", email = "jane@example.com", created_at = DateTime.UtcNow }
        };
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 2));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllBeOfType<UserDto>();
        result.Value.Items[0].Id.Should().Be(1);
        result.Value.Items[0].Name.Should().Be("John Doe");
        result.Value.Items[0].Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        const string searchTerm = "test";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var users = new List<UserEntity>();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 0));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        await _handler.HandleAsync(query, cancellationToken);

        // Assert
        _mockRepository.Verify(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData("john")]
    [InlineData("JOHN")]
    [InlineData("John")]
    [InlineData("특수문자!@#")]
    [InlineData("123456")]
    public async Task HandleAsync_WithDifferentSearchTerms_ShouldPassCorrectly(string searchTerm)
    {
        // Arrange
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var users = new List<UserEntity>();
        var repositoryResult = Result.Ok((Users: (IEnumerable<UserEntity>)users, TotalCount: 0));

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        await _handler.HandleAsync(query, cancellationToken);

        // Assert
        _mockRepository.Verify(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        const string searchTerm = "test";
        const int page = 1;
        const int pageSize = 10;
        var query = new UserListQuery(searchTerm, page, pageSize);
        var cancellationToken = CancellationToken.None;

        var repositoryResult = Result.Fail<(IEnumerable<UserEntity> Users, int TotalCount)>("Error 1").WithError("Error 2");

        _mockRepository
            .Setup(x => x.GetPagedAsync(searchTerm, page, pageSize, cancellationToken))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Select(e => e.Message).Should().Contain("Error 1");
        result.Errors.Select(e => e.Message).Should().Contain("Error 2");
    }
}