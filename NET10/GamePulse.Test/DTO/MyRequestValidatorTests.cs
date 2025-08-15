using FluentAssertions;
using Demo.Web.DTO;

namespace GamePulse.Test.DTO;

public class MyRequestValidatorTests
{
    private readonly MyRequestValidator _validator;

    public MyRequestValidatorTests()
    {
        _validator = new MyRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new MyRequest
        {
            FirstName = "홍",
            LastName = "길동",
            Age = 30
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "길동", 30)]
    [InlineData(null, "길동", 30)]
    public void Validate_InvalidFirstName_ShouldFail(string? firstName, string lastName, int age)
    {
        // Arrange
        var request = new MyRequest
        {
            FirstName = firstName!,
            LastName = lastName,
            Age = age
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MyRequest.FirstName));
    }

    [Theory]
    [InlineData("홍", "", 30)]
    [InlineData("홍", null, 30)]
    public void Validate_InvalidLastName_ShouldFail(string firstName, string? lastName, int age)
    {
        // Arrange
        var request = new MyRequest
        {
            FirstName = firstName,
            LastName = lastName!,
            Age = age
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MyRequest.LastName));
    }

    [Theory]
    [InlineData("홍", "길동", 0)]
    [InlineData("홍", "길동", -1)]
    [InlineData("홍", "길동", 151)]
    public void Validate_InvalidAge_ShouldFail(string firstName, string lastName, int age)
    {
        // Arrange
        var request = new MyRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Age = age
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MyRequest.Age));
    }
}