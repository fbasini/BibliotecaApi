using BibliotecaAPI.DTOs;
using BibliotecaAPITests.Utilities;
using System.Net;

namespace BibliotecaAPITests.IntegrationTesting.Controllers
{
    [TestClass]
    public class BooksControllerTests : TestBase
    {
        private readonly string url = "/api/books";
        private string nameDB = Guid.NewGuid().ToString();

        [TestMethod]
        public async Task Post_Returns400_WhenAuthorIdsAreEmpty()
        {
            // Arrange
            var factory = BuildWebApplicationFactory(nameDB);
            var client = factory.CreateClient();
            var createBookDTO = new CreateBookDTO { Title = "Title" };

            // Act
            var response = await client.PostAsJsonAsync(url, createBookDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.BadRequest, actual: response.StatusCode);
        }
    }
}
