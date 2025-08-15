using FluentAssertions;
using Demo.Web.DTO;

namespace GamePulse.Test.DTO;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "admin"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "admin")]
    [InlineData(null, "admin")]
    [InlineData("ab", "admin")]
    [InlineData("verylongusername", "admin")]
    public void Validate_InvalidUsername_ShouldFail(string? username, string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = username!,
            Password = password
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Theory]
    [InlineData("admin", "")]
    [InlineData("admin", null)]
    [InlineData("admin", "ab")]
    [InlineData("admin", "verylongpassword")]
    public void Validate_InvalidPassword_ShouldFail(string username, string? password)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = username,
            Password = password!
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
    }
}