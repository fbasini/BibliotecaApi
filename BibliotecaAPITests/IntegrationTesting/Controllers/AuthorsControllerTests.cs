using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPITests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BibliotecaAPITests.IntegrationTesting.Controllers
{
    [TestClass]
    public class AuthorsControllerTests : TestBase
    {
        private static readonly string url = "/api/authors";
        private string nameDB = Guid.NewGuid().ToString();


        [TestMethod]
        public async Task Get_Returns404_WhenAuthorDoesNotExist()
        {
            // Arrange
            var factory = BuildWebApplicationFactory(nameDB);
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync($"{url}/1");

            // Assert
            var statusCode = response.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: response.StatusCode);
        }

        [TestMethod]
        public async Task Get_ReturnsAuthor_WhenAuthorExists()
        {
            // Arrange
            var context = BuildContext(nameDB);
            context.Authors.Add(new Author() { FirstName = "Felipe", LastName = "Perez" });
            context.Authors.Add(new Author() { FirstName = "Juan", LastName = "Gomez" });
            await context.SaveChangesAsync();

            var factory = BuildWebApplicationFactory(nameDB);
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync($"{url}/1");

            // Assert
            response.EnsureSuccessStatusCode();

            var author = JsonSerializer.Deserialize<AuthorWithBooksDTO>(
                await response.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            Assert.AreEqual(expected: 1, author.Id);
        }

        [TestMethod]
        public async Task Post_Returns401_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var factory = BuildWebApplicationFactory(nameDB, ignoreSecurity: false);

            var client = factory.CreateClient();
            var createAuthorDTO = new CreateAuthorDTO
            {
                FirstNames = "Felipe",
                LastNames = "Perez",
                Identification = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync(url, createAuthorDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: response.StatusCode);
        }

        [TestMethod]
        public async Task Post_Returns403_WhenUserIsNotAdmin()
        {
            // Arrange
            var factory = BuildWebApplicationFactory(nameDB, ignoreSecurity: false);
            var token = await CreateUser(nameDB, factory);

            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createAuthorDTO = new CreateAuthorDTO
            {
                FirstNames = "Felipe",
                LastNames = "Perez",
                Identification = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync(url, createAuthorDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: response.StatusCode);
        }

        [TestMethod]
        public async Task Post_Returns201_WhenUserIsAdmin()
        {
            // Arrange
            var factory = BuildWebApplicationFactory(nameDB, ignoreSecurity: false);
            var claims = new List<Claim> { adminClaim };
            var token = await CreateUser(nameDB, factory, claims);

            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createAuthorDTO = new CreateAuthorDTO
            {
                FirstNames = "Felipe",
                LastNames = "Perez",
                Identification = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync(url, createAuthorDTO);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual(expected: HttpStatusCode.Created, actual: response.StatusCode);
        }
    }
}
