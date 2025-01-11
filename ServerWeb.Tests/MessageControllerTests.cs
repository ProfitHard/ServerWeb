using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Http;
using ServerWeb.BLL.Models;
using ServerWeb.Controllers;
using ServerWeb.DAL.Context;

namespace ServerWeb.Tests
{
    public class MessageControllerTests
    {
        private static Mock<IHttpContextAccessor> CreateHttpContextAccessorMock(int userId)
        {
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(x => x.FindFirst(ClaimTypes.NameIdentifier))
              .Returns(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
            return httpContextAccessorMock;
        }
        [Fact]
        public async Task PostMessage_ValidRequest_ReturnsCreatedAtActionResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(databaseName: "TestMessageDatabase")
             .Options;

            using var context = new AppDbContext(options);
            var user = new User { Username = "testUser", PasswordHash = "password" };
            var user2 = new User { Username = "testUser2", PasswordHash = "password" };
            context.Users.AddRange(user, user2);
            await context.SaveChangesAsync();
            var httpContextAccessorMock = CreateHttpContextAccessorMock(user.Id);
            var controller = new MessageController(context);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContextAccessorMock.Object.HttpContext };

            var messageCreateRequest = new MessageCreateRequest { Content = "Test message", ReceiverId = user2.Id };
            var result = await controller.PostMessage(messageCreateRequest);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var messageResponse = Assert.IsType<MessageResponse>(createdAtActionResult.Value);
            Assert.Equal("Test message", messageResponse.Content);

            var messageExists = context.Messages.Any();
            Assert.True(messageExists);

        }
        [Fact]
        public async Task PostMessage_InvalidReceiverId_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestMessageDatabase")
           .Options;

            using var context = new AppDbContext(options);
            var user = new User { Username = "testUser", PasswordHash = "password" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var httpContextAccessorMock = CreateHttpContextAccessorMock(user.Id);
            var controller = new MessageController(context);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContextAccessorMock.Object.HttpContext };
            var messageCreateRequest = new MessageCreateRequest { Content = "Test message", ReceiverId = 999 };

            var result = await controller.PostMessage(messageCreateRequest);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
        [Fact]
        public async Task PostMessage_UnauthorizedUser_ReturnsUnauthorizedResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(databaseName: "TestMessageDatabase")
              .Options;

            using var context = new AppDbContext(options);
            var httpContextAccessorMock = CreateHttpContextAccessorMock(999);

            var controller = new MessageController(context);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContextAccessorMock.Object.HttpContext };
            var messageCreateRequest = new MessageCreateRequest { Content = "Test message", ReceiverId = 1 };

            var result = await controller.PostMessage(messageCreateRequest);

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }
        [Fact]
        public async Task GetMessage_ValidId_ReturnsOkResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
             .UseInMemoryDatabase(databaseName: "TestMessageDatabase")
             .Options;

            using var context = new AppDbContext(options);
            var user = new User { Username = "testUser", PasswordHash = "password" };
            var user2 = new User { Username = "testUser2", PasswordHash = "password" };
            context.Users.AddRange(user, user2);
            var message = new Message { SenderId = user.Id, ReceiverId = user2.Id, Content = "testMessage" };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var controller = new MessageController(context);
            var result = await controller.GetMessage(message.Id);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var messageResponse = Assert.IsType<MessageResponse>(okResult.Value);
            Assert.Equal(message.Content, messageResponse.Content);
        }
        [Fact]
        public async Task GetMessage_InvalidId_ReturnsNotFoundResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestMessageDatabase")
            .Options;

            using var context = new AppDbContext(options);
            var controller = new MessageController(context);
            var result = await controller.GetMessage(999);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}