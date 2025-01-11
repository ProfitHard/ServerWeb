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
    public class PostControllerTests
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
        public async Task Post_ValidRequest_ReturnsCreatedAtActionResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(databaseName: "TestPostDatabase")
              .Options;
            using var context = new AppDbContext(options);
            var user = new User { Username = "testuser", PasswordHash = "test" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var httpContextAccessorMock = CreateHttpContextAccessorMock(user.Id);
            var controller = new PostController(context);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContextAccessorMock.Object.HttpContext };
            var postRequest = new PostCreateRequest { Content = "Test content" };

            var result = await controller.PostPost(postRequest);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var postResponse = Assert.IsType<PostResponse>(createdResult.Value);
            Assert.Equal("Test content", postResponse.Content);
            var postExists = context.Posts.Any();
            Assert.True(postExists);
        }
        [Fact]
        public async Task Post_UnauthorizedUser_ReturnsUnauthorizedResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestPostDatabase")
            .Options;

            using var context = new AppDbContext(options);

            var httpContextAccessorMock = CreateHttpContextAccessorMock(999);

            var controller = new PostController(context);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContextAccessorMock.Object.HttpContext };
            var postRequest = new PostCreateRequest { Content = "Test content" };

            var result = await controller.PostPost(postRequest);
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }
        [Fact]
        public async Task GetPost_ValidId_ReturnsOkResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
             .UseInMemoryDatabase(databaseName: "TestPostDatabase")
             .Options;

            using var context = new AppDbContext(options);
            var user = new User { Username = "testuser", PasswordHash = "test" };
            context.Users.Add(user);
            var post = new Post { AuthorId = user.Id, Content = "test post" };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var controller = new PostController(context);
            var result = await controller.GetPost(post.Id);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var postResponse = Assert.IsType<PostResponse>(okResult.Value);
            Assert.Equal(post.Content, postResponse.Content);
        }
        [Fact]
        public async Task GetPost_InvalidId_ReturnsNotFoundResult()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(databaseName: "TestPostDatabase")
              .Options;

            using var context = new AppDbContext(options);
            var controller = new PostController(context);

            var result = await controller.GetPost(999);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}
