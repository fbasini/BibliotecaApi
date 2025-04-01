using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.UnitTesting.Services
{
    [TestClass]
    public class UserServiceTests
    {
        private UserManager<User> userManager = null!;
        private IHttpContextAccessor contextAccessor = null!;
        private UserService userService = null!;

        [TestInitialize]
        public void Setup()
        {
            userManager = Substitute.For<UserManager<User>>(
                Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);

            contextAccessor = Substitute.For<IHttpContextAccessor>();
            userService = new UserService(userManager, contextAccessor);
        }

        [TestMethod]
        public async Task GetUser_ReturnsNull_WhenThereIsNoEmailClaim()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Returns(httpContext);

            // Act
            var user = await userService.GetUser();

            // Assert
            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task GetUser_ReturnsUser_WhenThereIsEmailClaim()
        {
            // Arrange
            var email = "test@hotmail.com";
            var expectedUser = new User { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult(expectedUser));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contextAccessor.HttpContext.Returns(httpContext);

            // Act
            var user = await userService.GetUser();

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual(expected: email, actual: user.Email);
        }

        [TestMethod]
        public async Task GetUser_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var email = "test@hotmail.com";
            var expectedUser = new User { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult<User>(null!));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contextAccessor.HttpContext.Returns(httpContext);

            // Act
            var user = await userService.GetUser();

            // Assert
            Assert.IsNull(user);
        }
    }
}
