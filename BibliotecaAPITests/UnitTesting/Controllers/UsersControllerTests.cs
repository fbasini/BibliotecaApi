using BibliotecaAPI.Controllers;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using BibliotecaAPITests.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.UnitTesting.Controllers
{
    [TestClass]
    public class UsersControllerTests : TestBase
    {
        private string nameDB = Guid.NewGuid().ToString();
        private UserManager<User> userManager = null!;
        private SignInManager<User> signInManager = null!;
        private UsersController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = BuildContext(nameDB);
            userManager = Substitute.For<UserManager<User>>(
                Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);

            var myConfiguration = new Dictionary<string, string>
            {
                {
                    "jwtkey", "askjdansjkdansjkdNJKANSDJKANSDJKASNDAJKSNDJ"
                }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration!)
                .Build();

            var contextAccessor = Substitute.For<IHttpContextAccessor>();
            var userClaimsFactory = Substitute.For<IUserClaimsPrincipalFactory<User>>();

            signInManager = Substitute.For<SignInManager<User>>(userManager,
                contextAccessor, userClaimsFactory, null, null, null, null);

            var userService = Substitute.For<IUserService>();

            var mapper = AutoMapperConfiguration();

            controller = new UsersController(context, mapper, userManager, configuration,
                signInManager, userService);
        }

        [TestMethod]
        public async Task Register_ReturnsValidationProblem_WhenNotSuccessful()
        {
            // Arrange
            var errorMessage = "test";
            var credentials = new UserCredentialsDTO
            {
                Email = "test@hotmail.com",
                Password = "aA123456!"
            };

            userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
                    .Returns(IdentityResult.Failed(new IdentityError
                    {
                        Code = "test",
                        Description = errorMessage
                    }));

            // Act
            var response = await controller.Register(credentials);

            // Assert
            var result = response.Result as ObjectResult;
            var problemDetails = result!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: errorMessage, actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Register_ReturnsToken_WhenSuccessful()
        {
            // Arrange
            var credentials = new UserCredentialsDTO
            {
                Email = "test@hotmail.com",
                Password = "aA123456!"
            };

            userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
                    .Returns(IdentityResult.Success);

            // Act
            var response = await controller.Register(credentials);

            // Assert
            Assert.IsNotNull(response.Value);
            Assert.IsNotNull(response.Value.Token);
        }

        [TestMethod]
        public async Task Login_ReturnsValidationProblem_WhenUserDoesNotExist()
        {
            // Arrange
            var credentials = new UserCredentialsDTO
            {
                Email = "test@hotmail.com",
                Password = "aA123456!"
            };

            userManager.FindByEmailAsync(credentials.Email)!.Returns(Task.FromResult<User>(null!));

            // Act
            var response = await controller.Login(credentials);

            // Assert
            var result = response.Result as ObjectResult;
            var problemDetails = result!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Invalid login",
                actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_ReturnsValidationProblem_WhenLoginIsNotSuccessful()
        {
            // Arrange
            var credentials = new UserCredentialsDTO
            {
                Email = "test@hotmail.com",
                Password = "aA123456!"
            };

            var user = new User { Email = credentials.Email };

            userManager.FindByEmailAsync(credentials.Email)!.Returns(Task.FromResult(user));

            signInManager.CheckPasswordSignInAsync(user, credentials.Password, false)
                .Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var response = await controller.Login(credentials);

            // Assert
            var result = response.Result as ObjectResult;
            var problemDetails = result!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Invalid login",
                actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_ReturnsToken_WhenLoginIsSuccessful()
        {
            // Arrange
            var credentials = new UserCredentialsDTO
            {
                Email = "test@hotmail.com",
                Password = "aA123456!"
            };

            var user = new User { Email = credentials.Email };

            userManager.FindByEmailAsync(credentials.Email)!.Returns(Task.FromResult(user));

            signInManager.CheckPasswordSignInAsync(user, credentials.Password, false)
                .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var response = await controller.Login(credentials);

            // Assert
            Assert.IsNotNull(response.Value);
            Assert.IsNotNull(response.Value.Token);
        }
    }
}
