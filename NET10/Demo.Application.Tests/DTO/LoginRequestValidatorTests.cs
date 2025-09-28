using Demo.Application.DTO;
using FastEndpoints;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Demo.Application.Tests.DTO;

/// <summary>
/// LoginRequestValidator 클래스의 단위 테스트
/// 로그인 요청 유효성 검사 로직 테스트
/// </summary>
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    #region Username Validation Tests

    [Fact]
    public void Username_WithValidValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest { Username = "john", Password = "validpass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("abc")]      // Minimum length (3)
    [InlineData("abcdefgh")] // Maximum length (8)
    [InlineData("user123")]  // With numbers
    [InlineData("TestUser")] // Mixed case
    public void Username_WithValidLengths_ShouldNotHaveValidationError(string username)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = "validpass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Username_WhenNullOrEmpty_ShouldHaveValidationError(string? username)
    {
        // Arrange
        var request = new LoginRequest { Username = username!, Password = "validpass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (username is null)
        {
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("사용자명은 null일 수 없습니다");
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("사용자명은 필수입니다");
        }
    }

    [Theory]
    [InlineData("ab")]          // Too short (2 characters)
    [InlineData("a")]           // Too short (1 character)
    public void Username_WhenTooShort_ShouldHaveValidationError(string username)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = "validpass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("사용자명은 너무 짧습니다");
    }

    [Theory]
    [InlineData("abcdefghi")]   // Too long (9 characters)
    [InlineData("verylongusername")] // Much too long
    public void Username_WhenTooLong_ShouldHaveValidationError(string username)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = "validpass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("사용자명은 너무 깁니다");
    }

    [Theory]
    [InlineData("   ")]         // Whitespace only
    [InlineData("\t\t\t")]      // Tabs only
    [InlineData("\n\n\n")]      // Newlines only
    [InlineData(" \t \n ")]     // Mixed whitespace
    public void Username_WhenWhitespaceOnly_ShouldHaveValidationError(string username)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = "validpass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("사용자명은 필수입니다");
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public void Password_WithValidValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest { Username = "validuser", Password = "pass" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("abc")]      // Minimum length (3)
    [InlineData("abcdefgh")] // Maximum length (8)
    [InlineData("pass123")]  // With numbers
    [InlineData("PassWord")] // Mixed case
    [InlineData("p@ss!")]    // With special characters
    public void Password_WithValidLengths_ShouldNotHaveValidationError(string password)
    {
        // Arrange
        var request = new LoginRequest { Username = "validuser", Password = password };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Password_WhenNullOrEmpty_ShouldHaveValidationError(string? password)
    {
        // Arrange
        var request = new LoginRequest { Username = "validuser", Password = password! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (password is null)
        {
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("비밀번호는 null일 수 없습니다");
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("비밀번호는 필수입니다");
        }
    }

    [Theory]
    [InlineData("ab")]          // Too short (2 characters)
    [InlineData("a")]           // Too short (1 character)
    public void Password_WhenTooShort_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new LoginRequest { Username = "validuser", Password = password };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("비밀번호는 너무 짧습니다");
    }

    [Theory]
    [InlineData("abcdefghi")]   // Too long (9 characters)
    [InlineData("verylongpassword")] // Much too long
    public void Password_WhenTooLong_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new LoginRequest { Username = "validuser", Password = password };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("비밀번호는 너무 깁니다");
    }

    [Theory]
    [InlineData("   ")]         // Whitespace only
    [InlineData("\t\t\t")]      // Tabs only
    [InlineData("\n\n\n")]      // Newlines only
    [InlineData(" \t \n ")]     // Mixed whitespace
    public void Password_WhenWhitespaceOnly_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new LoginRequest { Username = "validuser", Password = password };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("비밀번호는 필수입니다");
    }

    #endregion

    #region Combined Validation Tests

    [Fact]
    public void LoginRequest_WithBothValidUsernameAndPassword_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new LoginRequest { Username = "john", Password = "pass123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginRequest_WithBothInvalidUsernameAndPassword_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var request = new LoginRequest { Username = "", Password = "" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void LoginRequest_WithValidUsernameButInvalidPassword_ShouldHavePasswordValidationError()
    {
        // Arrange
        var request = new LoginRequest { Username = "valid", Password = "ab" }; // Password too short

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void LoginRequest_WithInvalidUsernameButValidPassword_ShouldHaveUsernameValidationError()
    {
        // Arrange
        var request = new LoginRequest { Username = "ab", Password = "valid" }; // Username too short

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void LoginRequest_WithExactlyMinimumLength_ShouldBeValid()
    {
        // Arrange
        var request = new LoginRequest { Username = "abc", Password = "def" }; // Both exactly 3 characters

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginRequest_WithExactlyMaximumLength_ShouldBeValid()
    {
        // Arrange
        var request = new LoginRequest { Username = "abcdefgh", Password = "12345678" }; // Both exactly 8 characters

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("user@123", "pass!@#")]  // Special characters
    [InlineData("한글사용자", "한글비번")]      // Unicode characters
    [InlineData("123456", "abcdef")]     // Numbers and letters
    public void LoginRequest_WithSpecialCharacters_ShouldBeValidIfWithinLengthLimits(string username, string password)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = password };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (username.Length >= 3 && username.Length <= 8 && password.Length >= 3 && password.Length <= 8)
        {
            result.ShouldNotHaveAnyValidationErrors();
        }
        else
        {
            // Should have validation errors for length constraints
            if (username.Length < 3 || username.Length > 8)
                result.ShouldHaveValidationErrorFor(x => x.Username);
            if (password.Length < 3 || password.Length > 8)
                result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }

    [Fact]
    public void LoginRequest_WithLeadingAndTrailingSpaces_ShouldBeValidIfContentIsValid()
    {
        // Arrange - Note: FluentValidation doesn't trim by default
        var request = new LoginRequest { Username = " user ", Password = " pass " }; // 6 characters each including spaces

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}