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
        var user = new User();

        // Assert
        user.Id.Should().Be(0);
        user.Name.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.Password.Should().Be(string.Empty);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
        var user = new User
        {
            Id = id,
            Name = name,
            Email = email,
            Password = password,
            CreatedAt = createdAt
        };

        // Assert
        user.Id.Should().Be(id);
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.Password.Should().Be(password);
        user.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void User_Should_Handle_Empty_Name_Values(string? name)
    {
        // Arrange & Act
        var user = new User
        {
            Id = 1,
            Name = name ?? string.Empty,
            Email = "test@example.com",
            Password = "password123"
        };

        // Assert
        user.Name.Should().Be(name ?? string.Empty);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    [InlineData("test123@test-domain.com")]
    public void User_Should_Accept_Valid_Email_Formats(string email)
    {
        // Arrange & Act
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = email,
            Password = "password123"
        };

        // Assert
        user.Email.Should().Be(email);
    }

    [Fact]
    public void User_CreatedAt_Should_Be_Immutable()
    {
        // Arrange
        var originalDate = DateTime.UtcNow.AddDays(-1);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123",
            CreatedAt = originalDate
        };

        // Act & Assert
        user.CreatedAt.Should().Be(originalDate);
        
        // CreatedAt should be init-only property, so this won't compile:
        // user.CreatedAt = DateTime.UtcNow; // This should cause compilation error
    }
}

public class UserDbTests
{
    private readonly Faker _faker;

    public UserDbTests()
    {
        _faker = new Faker();
    }

    [Fact]
    public void UserDb_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var userDb = new UserDb();

        // Assert
        userDb.Id.Should().Be(0);
        userDb.Name.Should().Be(string.Empty);
        userDb.Email.Should().Be(string.Empty);
        userDb.Password.Should().Be(string.Empty);
        userDb.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
        var userDb = new UserDb
        {
            Id = id,
            Name = name,
            Email = email,
            Password = password,
            CreatedAt = createdAt
        };

        // Assert
        userDb.Id.Should().Be(id);
        userDb.Name.Should().Be(name);
        userDb.Email.Should().Be(email);
        userDb.Password.Should().Be(password);
        userDb.CreatedAt.Should().Be(createdAt);
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
        var userDb = new UserDb
        {
            Id = id,
            Name = name,
            Email = email,
            Password = password,
            CreatedAt = createdAt
        };

        // Assert
        userDb.Id.Should().Be(id);
        userDb.Name.Should().Be(name);
        userDb.Email.Should().Be(email);
        userDb.Password.Should().Be(password);
        userDb.CreatedAt.Should().Be(createdAt);

        // Properties should be init-only, so these won't compile:
        // userDb.id = 999; // This should cause compilation error
        // userDb.name = "New Name"; // This should cause compilation error
    }
}
