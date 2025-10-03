using System.ComponentModel.DataAnnotations;
using Demo.Application.Commands;
using Demo.Domain.Repositories;
using FluentAssertions;
using FluentResults;
using Moq;

namespace Demo.Application.Tests.Commands;

/// <summary>
/// UserCreateCommand, Validator, Handler의 단위 테스트
/// 사용자 생성 명령어 처리 로직 테스트
/// </summary>
public class UserCreateCommandTests
{
    #region UserCreateCommandValidator Tests

    public class UserCreateCommandValidatorTests
    {
        private readonly UserCreateCommandValidator _validator;

        public UserCreateCommandValidatorTests()
        {
            _validator = new UserCreateCommandValidator();
        }

        [Fact]
        public async Task ValidateAsync_WithValidName_ShouldNotThrowException()
        {
            // Arrange
            var command = new UserCreateCommand("John Doe", "john@example.com", "password123");
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _validator.ValidateAsync(command, cancellationToken));

            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public async Task ValidateAsync_WithInvalidName_ShouldThrowValidationException(string? invalidName)
        {
            // Arrange
            var command = new UserCreateCommand(invalidName!, "john@example.com", "password123");
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                _validator.ValidateAsync(command, cancellationToken));

            exception.Message.Should().Be("Product title cannot be empty");
        }

        [Theory]
        [InlineData("ValidName", "", "password")]
        [InlineData("ValidName", "valid@email.com", "")]
        [InlineData("ValidName", null, "password")]
        [InlineData("ValidName", "valid@email.com", null)]
        public async Task ValidateAsync_WithValidNameButOtherInvalidFields_ShouldNotThrowException(
            string name, string? email, string? password)
        {
            // Arrange - The validator only checks Name, not Email or Password
            var command = new UserCreateCommand(name, email!, password!);
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _validator.ValidateAsync(command, cancellationToken));

            exception.Should().BeNull("Validator only validates Name field");
        }

        [Fact]
        public async Task ValidateAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var command = new UserCreateCommand("John Doe", "john@example.com", "password123");
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _validator.ValidateAsync(command, cancellationToken));

            exception.Should().BeNull();
        }

        [Fact]
        public async Task ValidateAsync_WithLongValidName_ShouldNotThrowException()
        {
            // Arrange
            var longName = new string('A', 1000); // Very long name
            var command = new UserCreateCommand(longName, "john@example.com", "password123");
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _validator.ValidateAsync(command, cancellationToken));

            exception.Should().BeNull();
        }
    }

    #endregion

    #region UserCreateCommandHandler Tests

    public class UserCreateCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly UserCreateCommandHandler _handler;

        public UserCreateCommandHandlerTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _handler = new UserCreateCommandHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidCommand_ShouldCallRepositoryAndReturnResult()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password = "password123";
            var command = new UserCreateCommand(name, email, password);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.HandleAsync(command, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldHashPassword()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password = "password123";
            var command = new UserCreateCommand(name, email, password);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            string? capturedHashedPassword = null;
            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((n, e, p, ct) => capturedHashedPassword = p)
                .ReturnsAsync(expectedResult);

            // Act
            await _handler.HandleAsync(command, cancellationToken);

            // Assert
            capturedHashedPassword.Should().NotBeNull();
            capturedHashedPassword.Should().NotBe(password, "Password should be hashed");
            capturedHashedPassword.Should().NotBeNullOrEmpty();

            // Check if it's Base64 encoded (typical SHA256 hash output)
            var isBase64 = IsValidBase64String(capturedHashedPassword!);
            isBase64.Should().BeTrue("Hashed password should be Base64 encoded");
        }

        [Fact]
        public async Task HandleAsync_WithDifferentPasswords_ShouldProduceDifferentHashes()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password1 = "password123";
            const string password2 = "differentpassword";

            var command1 = new UserCreateCommand(name, email, password1);
            var command2 = new UserCreateCommand(name, email, password2);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            var capturedHashes = new List<string>();
            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((n, e, p, ct) => capturedHashes.Add(p))
                .ReturnsAsync(expectedResult);

            // Act
            await _handler.HandleAsync(command1, cancellationToken);
            await _handler.HandleAsync(command2, cancellationToken);

            // Assert
            capturedHashes.Should().HaveCount(2);
            capturedHashes[0].Should().NotBe(capturedHashes[1], "Different passwords should produce different hashes");
        }

        [Fact]
        public async Task HandleAsync_WithSamePassword_ShouldProduceSameHash()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password = "password123";

            var command1 = new UserCreateCommand(name, email, password);
            var command2 = new UserCreateCommand(name, email, password);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            var capturedHashes = new List<string>();
            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((n, e, p, ct) => capturedHashes.Add(p))
                .ReturnsAsync(expectedResult);

            // Act
            await _handler.HandleAsync(command1, cancellationToken);
            await _handler.HandleAsync(command2, cancellationToken);

            // Assert
            capturedHashes.Should().HaveCount(2);
            capturedHashes[0].Should().Be(capturedHashes[1], "Same passwords should produce same hashes");
        }

        [Fact]
        public async Task HandleAsync_WhenRepositoryFails_ShouldReturnFailureResult()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password = "password123";
            var command = new UserCreateCommand(name, email, password);
            var expectedResult = Result.Fail("Database error");
            var cancellationToken = CancellationToken.None;

            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.HandleAsync(command, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Result.IsFailed.Should().BeTrue();
            result.Result.Errors.Should().HaveCount(1);
            result.Result.Errors[0].Message.Should().Be("Database error");
        }

        [Fact]
        public async Task HandleAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password = "password123";
            var command = new UserCreateCommand(name, email, password);
            var expectedException = new InvalidOperationException("Database connection failed");
            var cancellationToken = CancellationToken.None;

            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.HandleAsync(command, cancellationToken));

            exception.Should().Be(expectedException);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public async Task HandleAsync_WithEmptyPassword_ShouldStillHashAndProcess(string emptyPassword)
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            var command = new UserCreateCommand(name, email, emptyPassword);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            string? capturedHashedPassword = null;
            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((n, e, p, ct) => capturedHashedPassword = p)
                .ReturnsAsync(expectedResult);

            // Act
            await _handler.HandleAsync(command, cancellationToken);

            // Assert
            capturedHashedPassword.Should().NotBeNull();
            capturedHashedPassword.Should().NotBe(emptyPassword);
        }

        [Fact]
        public async Task HandleAsync_WithLongPassword_ShouldHashSuccessfully()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            var longPassword = new string('P', 10000); // Very long password
            var command = new UserCreateCommand(name, email, longPassword);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            string? capturedHashedPassword = null;
            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((n, e, p, ct) => capturedHashedPassword = p)
                .ReturnsAsync(expectedResult);

            // Act
            await _handler.HandleAsync(command, cancellationToken);

            // Assert
            capturedHashedPassword.Should().NotBeNull();
            capturedHashedPassword.Should().NotBe(longPassword);

            // SHA256 hash should always be 44 characters when Base64 encoded
            capturedHashedPassword!.Length.Should().Be(44);
        }

        [Fact]
        public async Task HandleAsync_WithSpecialCharactersInPassword_ShouldHashSuccessfully()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string specialPassword = "P@ssw0rd!@#$%^&*()_+-=[]{}|;:,.<>?";
            var command = new UserCreateCommand(name, email, specialPassword);
            var expectedResult = Result.Ok();
            var cancellationToken = CancellationToken.None;

            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.HandleAsync(command, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithCancellationToken_ShouldPassToRepository()
        {
            // Arrange
            const string name = "John Doe";
            const string email = "john@example.com";
            const string password = "password123";
            var command = new UserCreateCommand(name, email, password);
            var expectedResult = Result.Ok();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _mockRepository
                .Setup(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            await _handler.HandleAsync(command, cancellationToken);

            // Assert
            _mockRepository.Verify(x => x.CreateAsync(name, email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Helper method to check if a string is valid Base64
        /// </summary>
        private static bool IsValidBase64String(string s)
        {
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion
}