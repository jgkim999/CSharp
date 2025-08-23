using Demo.Domain.Entities;
using Demo.Domain.Repositories;
using FluentAssertions;
using FluentResults;
using Moq;
using Bogus;

namespace Demo.Domain.Tests.Repositories;

public class IUserRepositoryTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Faker _faker;

    public IUserRepositoryTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _faker = new Faker();
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Success_Result_When_User_Created_Successfully()
    {
        // Arrange
        var name = _faker.Person.FullName;
        var email = _faker.Person.Email;
        var passwordHash = _faker.Internet.Password();
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(r => r.CreateAsync(name, email, passwordHash, cancellationToken))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _mockRepository.Object.CreateAsync(name, email, passwordHash, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();

        _mockRepository.Verify(
            r => r.CreateAsync(name, email, passwordHash, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Failure_Result_When_User_Creation_Fails()
    {
        // Arrange
        var name = _faker.Person.FullName;
        var email = _faker.Person.Email;
        var passwordHash = _faker.Internet.Password();
        var cancellationToken = CancellationToken.None;
        var errorMessage = "Email already exists";

        _mockRepository
            .Setup(r => r.CreateAsync(name, email, passwordHash, cancellationToken))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act
        var result = await _mockRepository.Object.CreateAsync(name, email, passwordHash, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Message.Should().Be(errorMessage);

        _mockRepository.Verify(
            r => r.CreateAsync(name, email, passwordHash, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Success_Result_With_Users_When_Users_Exist()
    {
        // Arrange
        var limit = 10;
        var cancellationToken = CancellationToken.None;
        var users = GenerateUsers(5);

        _mockRepository
            .Setup(r => r.GetAllAsync(limit, cancellationToken))
            .ReturnsAsync(Result.Ok<IEnumerable<User>>(users));

        // Act
        var result = await _mockRepository.Object.GetAllAsync(limit, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(5);
        result.Value.Should().BeEquivalentTo(users);

        _mockRepository.Verify(
            r => r.GetAllAsync(limit, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_Collection_When_No_Users_Exist()
    {
        // Arrange
        var limit = 10;
        var cancellationToken = CancellationToken.None;
        var emptyUsers = Enumerable.Empty<User>();

        _mockRepository
            .Setup(r => r.GetAllAsync(limit, cancellationToken))
            .ReturnsAsync(Result.Ok<IEnumerable<User>>(emptyUsers));

        // Act
        var result = await _mockRepository.Object.GetAllAsync(limit, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();

        _mockRepository.Verify(
            r => r.GetAllAsync(limit, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Failure_Result_When_Database_Error_Occurs()
    {
        // Arrange
        var limit = 10;
        var cancellationToken = CancellationToken.None;
        var errorMessage = "Database connection failed";

        _mockRepository
            .Setup(r => r.GetAllAsync(limit, cancellationToken))
            .ReturnsAsync(Result.Fail<IEnumerable<User>>(errorMessage));

        // Act
        var result = await _mockRepository.Object.GetAllAsync(limit, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Message.Should().Be(errorMessage);

        _mockRepository.Verify(
            r => r.GetAllAsync(limit, cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task GetAllAsync_Should_Respect_Limit_Parameter(int limit)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var users = GenerateUsers(limit);

        _mockRepository
            .Setup(r => r.GetAllAsync(limit, cancellationToken))
            .ReturnsAsync(Result.Ok<IEnumerable<User>>(users));

        // Act
        var result = await _mockRepository.Object.GetAllAsync(limit, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(limit);

        _mockRepository.Verify(
            r => r.GetAllAsync(limit, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        var name = _faker.Person.FullName;
        var email = _faker.Person.Email;
        var passwordHash = _faker.Internet.Password();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockRepository
            .Setup(r => r.CreateAsync(name, email, passwordHash, cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _mockRepository.Object.CreateAsync(name, email, passwordHash, cancellationTokenSource.Token));

        _mockRepository.Verify(
            r => r.CreateAsync(name, email, passwordHash, cancellationTokenSource.Token),
            Times.Once);
    }

    private List<User> GenerateUsers(int count)
    {
        var users = new List<User>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(new User
            {
                Id = i,
                Name = _faker.Person.FullName,
                Email = _faker.Person.Email,
                Password = _faker.Internet.Password(),
                CreatedAt = _faker.Date.Recent()
            });
        }
        return users;
    }
}