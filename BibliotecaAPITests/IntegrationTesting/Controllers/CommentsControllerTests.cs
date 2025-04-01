using BibliotecaAPI.Entities;
using System.Net.Http.Headers;
using System.Net;
using BibliotecaAPITests.Utilities;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPITests.IntegrationTesting.Controllers
{
    [TestClass]
    public class CommentsControllerTests : TestBase
    {
        private readonly string url = "/api/books/1/comments";
        private string nameDB = Guid.NewGuid().ToString();

        private async Task CreatesTestData()
        {
            var context = BuildContext(nameDB);
            var author = new Author { FirstName = "Felipe", LastName = "Perez" };
            context.Add(author);
            await context.SaveChangesAsync();

            var book = new Book { Title = "title" };
            book.Authors.Add(new AuthorBook { Author = author });
            context.Add(book);
            await context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task Delete_Returns204_WhenUserDeletesOwnComment()
        {
            // Arrange
            await CreatesTestData();

            var factory = BuildWebApplicationFactory(nameDB, ignoreSecurity: false);

            var token = await CreateUser(nameDB, factory);

            var context = BuildContext(nameDB);
            var user = await context.Users.FirstAsync();

            var comment = new Comment
            {
                Body = "content",
                UserId = user!.Id,
                BookId = 1
            };

            context.Add(comment);
            await context.SaveChangesAsync();

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.DeleteAsync($"{url}/{comment.Id}");

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.NoContent, actual: response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_Returns403_WhenUserTriesToDeleteAnotherUsersComment()
        {
            // Arrange
            await CreatesTestData();

            var factory = BuildWebApplicationFactory(nameDB, ignoreSecurity: false);

            var commentCreatorEmail = "comment-creator@hotmail.com";
            await CreateUser(nameDB, factory, [], commentCreatorEmail);

            var context = BuildContext(nameDB);
            var commentCreatorUser = await context.Users.FirstAsync();

            var comment = new Comment
            {
                Body = "content",
                UserId = commentCreatorUser!.Id,
                BookId = 1
            };

            context.Add(comment);
            await context.SaveChangesAsync();

            var differentUserToken = await CreateUser(nameDB, factory, [], "different-user@hotmail.com");

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", differentUserToken);

            // Act
            var response = await client.DeleteAsync($"{url}/{comment.Id}");

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: response.StatusCode);
        }
    }
}
