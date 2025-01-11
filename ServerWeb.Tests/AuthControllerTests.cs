using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading.Tasks;
using Xunit;
using ServerWeb;
using ServerWeb.Controllers;
using Microsoft.Extensions.Configuration;
using Castle.Core.Configuration;
using ServerWeb.DAL.Context;
using ServerWeb.BLL.Models;

namespace ServerWeb.Tests
{
    public class AuthControllerTests
    {
        private Mock<Microsoft.Extensions.Configuration.IConfiguration> _configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        [Fact]
        public async Task Register_ValidRequest_ReturnsCreatedResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(databaseName: "TestAuthDatabase")
              .Options;

            using var context = new AppDbContext(options);
            var controller = new UserAuthController(context, _configurationMock.Object);


            var registerRequest = new RegisterRequest
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            var result = await controller.Register(registerRequest);
            var createdResult = Assert.IsType<CreatedResult>(result);
            var userResponse = Assert.IsType<UserResponse>(createdResult.Value);
            Assert.Equal("testuser", userResponse.UserName);
            Assert.NotEqual(0, userResponse.Id);

            var userExists = context.Users.Any(u => u.Username == "testuser");
            Assert.True(userExists);
        }
        [Fact]
        public async Task Register_ExistingUser_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestAuthDatabase")
                .Options;

            using var context = new AppDbContext(options);
            var controller = new UserAuthController(context, _configurationMock.Object);

            var registerRequest = new RegisterRequest
            {
                UserName = "testuser",
                Password = "testpassword"
            };
            context.Users.Add(new User { Username = "testuser", PasswordHash = "test" });
            await context.SaveChangesAsync();

            var result = await controller.Register(registerRequest);
            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
               .UseInMemoryDatabase(databaseName: "TestAuthDatabase")
               .Options;

            using var context = new AppDbContext(options);
            var controller = new UserAuthController(context, _configurationMock.Object);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("testpassword");
            context.Users.Add(new User { Username = "testuser", PasswordHash = passwordHash });
            await context.SaveChangesAsync();

            var loginRequest = new LoginRequest
            {
                UserName = "testuser",
                Password = "testpassword"
            };
            var result = await controller.Login(loginRequest);
            Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task Login_InvalidUsername_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestAuthDatabase")
                .Options;

            using var context = new AppDbContext(options);
            var controller = new UserAuthController(context, _configurationMock.Object);


            var loginRequest = new LoginRequest
            {
                UserName = "notTestuser",
                Password = "testpassword"
            };

            var result = await controller.Login(loginRequest);
            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task Login_InvalidPassword_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(databaseName: "TestAuthDatabase")
              .Options;

            using var context = new AppDbContext(options);
            var controller = new UserAuthController(context, _configurationMock.Object);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("testpassword");
            context.Users.Add(new User { Username = "testuser", PasswordHash = passwordHash });
            await context.SaveChangesAsync();

            var loginRequest = new LoginRequest
            {
                UserName = "testuser",
                Password = "wrongpassword"
            };
            var result = await controller.Login(loginRequest);
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}