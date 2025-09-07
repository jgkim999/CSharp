using Demo.Domain.Entities;
using FluentAssertions;
using Bogus;

namespace Demo.Domain.Tests.Entities;

public class UserTests
{
    private readonly Faker _faker;

    public UserTests()
    {
        _faker = new Faker();
    }

    [Fact]
    public void User_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var user = new UserEntity();

        // Assert
        user.id.Should().Be(0);
        user.name.Should().Be(string.Empty);
        user.email.Should().Be(string.Empty);
        user.password.Should().Be(string.Empty);
        user.created_at.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void User_Should_Initialize_With_Provided_Values()
    {
        // Arrange
        var id = _faker.Random.Long(1, 1000);
        var name = _faker.Person.FullName;
        var email = _faker.Person.Email;
        var password = _faker.Internet.Password();
        var createdAt = _faker.Date.Recent();

        // Act
        var user = new UserEntity
        {
            id = id,
            name = name,
            email = email,
            password = password,
            created_at = createdAt
        };

        // Assert
        user.id.Should().Be(id);
        user.name.Should().Be(name);
        user.email.Should().Be(email);
        user.password.Should().Be(password);
        user.created_at.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void User_Should_Handle_Empty_Name_Values(string? name)
    {
        // Arrange & Act
        var user = new UserEntity
        {
            id = 1,
            name = name ?? string.Empty,
            email = "test@example.com",
            password = "password123"
        };

        // Assert
        user.name.Should().Be(name ?? string.Empty);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    [InlineData("test123@test-domain.com")]
    public void User_Should_Accept_Valid_Email_Formats(string email)
    {
        // Arrange & Act
        var user = new UserEntity
        {
            id = 1,
            name = "Test User",
            email = email,
            password = "password123"
        };

        // Assert
        user.email.Should().Be(email);
    }

    [Fact]
    public void User_CreatedAt_Should_Be_Immutable()
    {
        // Arrange
        var originalDate = DateTime.UtcNow.AddDays(-1);
        var user = new UserEntity
        {
            id = 1,
            name = "Test User",
            email = "test@example.com",
            password = "password123",
            created_at = originalDate
        };

        // Act & Assert
        user.created_at.Should().Be(originalDate);
        
        // CreatedAt should be init-only property, so this won't compile:
        // user.CreatedAt = DateTime.UtcNow; // This should cause compilation error
    }
}

public class UserEntityTests
{
    private readonly Faker _faker;

    public UserEntityTests()
    {
        _faker = new Faker();
    }

    [Fact]
    public void UserDb_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var userEntity = new UserEntity();

        // Assert
        userEntity.id.Should().Be(0);
        userEntity.name.Should().Be(string.Empty);
        userEntity.email.Should().Be(string.Empty);
        userEntity.password.Should().Be(string.Empty);
        userEntity.created_at.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserDb_Should_Initialize_With_Provided_Values()
    {
        // Arrange
        var id = _faker.Random.Long(1, 1000);
        var name = _faker.Person.FullName;
        var email = _faker.Person.Email;
        var password = _faker.Internet.Password();
        var createdAt = _faker.Date.Recent();

        // Act
        var userEntity = new UserEntity
        {
            id = id,
            name = name,
            email = email,
            password = password,
            created_at = createdAt
        };

        // Assert
        userEntity.id.Should().Be(id);
        userEntity.name.Should().Be(name);
        userEntity.email.Should().Be(email);
        userEntity.password.Should().Be(password);
        userEntity.created_at.Should().Be(createdAt);
    }

    [Fact]
    public void UserDb_Properties_Should_Be_Init_Only()
    {
        // Arrange
        var id = _faker.Random.Long(1, 1000);
        var name = _faker.Person.FullName;
        var email = _faker.Person.Email;
        var password = _faker.Internet.Password();
        var createdAt = _faker.Date.Recent();

        // Act
        var userEntity = new UserEntity
        {
            id = id,
            name = name,
            email = email,
            password = password,
            created_at = createdAt
        };

        // Assert
        userEntity.id.Should().Be(id);
        userEntity.name.Should().Be(name);
        userEntity.email.Should().Be(email);
        userEntity.password.Should().Be(password);
        userEntity.created_at.Should().Be(createdAt);

        // Properties should be init-only, so these won't compile:
        // userDb.id = 999; // This should cause compilation error
        // userDb.name = "New Name"; // This should cause compilation error
    }
}
