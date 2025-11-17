using Auth.API;
using Auth.Core.Entities;
using Auth.Core.Interfaces.Repositories;
using Auth.IntegrationTests.DTOs;
using Auth.IntegrationTests.Helpers;
using FluentAssertions;
using Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Auth.IntegrationTests
{
    public class AuthIntegrationTests : BaseIntegrationTests<AuthApiFactory, Program>
    {
        public AuthIntegrationTests(AuthApiFactory factory) : base(factory)
        {
        }

        public override Task InitializeAsync() => Task.CompletedTask;
        public override Task DisposeAsync() => _resetState();

        [Fact]
        public async Task Register_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var request = new
            {
                username = "testuser",
                email = "test@example.com",
                password = "Password123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", request);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.UserId.Should().NotBeEmpty();
            result.Username.Should().Be(request.username);
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsConflict()
        {
            // Arrange
            var request1 = new
            {
                username = "existinguser1",
                email = "test1@example.com",
                password = "Password123!"
            };

            var request2 = new
            {
                username = "existinguser2",
                email = "test1@example.com",
                password = "Password123!"
            };

            // Act
            await _client.PostAsJsonAsync("/api/auth/register", request1);
            var response = await _client.PostAsJsonAsync("/api/auth/register", request2);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            content.Should().Contain("Email already exists");
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var registerRequest = new
            {
                username = "loginuser",
                email = "login@example.com",
                password = "Password123!"
            };

            var loginRequest = new
            {
                email = "login@example.com",
                password = "Password123!"
            };

            // Act
            await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.UserId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                email = "nonexistent",
                password = "wrongpassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            content.Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsSuccess()
        {
            // Arrange
            var registerRequest = new
            {
                username = "validateuser",
                email = "validate@example.com",
                password = "Password123!"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            var registerContent = await registerResponse.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(registerContent, _jsonOptions);

            var validateRequest = new { token = authResponse!.Token };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/validate", validateRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ValidateResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var validateRequest = new { token = "invalid.token.here" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/validate", validateRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ValidateResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task UserRepository_CanCreateAndRetrieveUser()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "repouser",
                Email = "repo@example.com",
                PasswordHash = "hashed_password",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var createdUser = await userRepository.AddAsync(user);
            var retrievedUser = await userRepository.GetByIdAsync(createdUser.Id);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Username.Should().Be(user.Username);
            retrievedUser.Email.Should().Be(user.Email);
        }
    }
}
