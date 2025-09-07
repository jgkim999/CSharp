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

    private List<UserEntity> GenerateUsers(int count)
    {
        var users = new List<UserEntity>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(new UserEntity
            {
                id = i,
                name = _faker.Person.FullName,
                email = _faker.Person.Email,
                password = _faker.Internet.Password(),
                created_at = _faker.Date.Recent()
            });
        }
        return users;
    }
}
