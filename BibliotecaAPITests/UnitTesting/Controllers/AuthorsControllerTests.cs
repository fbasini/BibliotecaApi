using BibliotecaAPI.Controllers;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using BibliotecaAPITests.Utilities;
using BibliotecaAPITests.Utilities.Doubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.UnitTesting.Controllers
{
    [TestClass]
    public class AuthorsControllerTests : TestBase
    {
        IFileStorage fileStorage = null!;
        ILogger<AuthorsController> logger = null!;
        IOutputCacheStore outputCacheStore = null!;
        IAuthorService authorService = null!;
        private string nameDB = Guid.NewGuid().ToString();
        private AuthorsController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = BuildContext(nameDB);
            var mapper = AutoMapperConfiguration();
            fileStorage = Substitute.For<IFileStorage>();
            logger = Substitute.For<ILogger<AuthorsController>>();
            outputCacheStore = Substitute.For<IOutputCacheStore>();
            authorService = Substitute.For<IAuthorService>();

            controller = new AuthorsController(context, mapper, fileStorage,
                logger, outputCacheStore, authorService);
        }

        [TestMethod]
        public async Task Get_Returns404_WhenAuthorWithIdDoesNotExist()
        {
            // Act
            var response = await controller.Get(1);

            // Assert
            var result = response.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: result!.StatusCode);
        }

        [TestMethod]
        public async Task Get_ReturnsAuthor_WhenAuthorWithIdExists()
        {
            // Arrange
            var context = BuildContext(nameDB);
            context.Authors.Add(new Author { FirstName = "Felipe", LastName = "Gomez" });
            context.Authors.Add(new Author { FirstName = "Juan", LastName = "Perez" });
            await context.SaveChangesAsync();

            // Act
            var response = await controller.Get(1);

            // Assert
            var result = response.Value;
            Assert.AreEqual(expected: 1, actual: result!.Id);
        }

        [TestMethod]
        public async Task Get_ReturnsAuthorWithBooks_WhenAuthorHasBooks()
        {
            // Arrange
            var context = BuildContext(nameDB);
            var book1 = new Book { Title = "Book 1" };
            var book2 = new Book { Title = "Book 2" };

            var author = new Author()
            {
                FirstName = "Felipe",
                LastName = "Perez",
                Books = new List<AuthorBook>
                {
                    new AuthorBook{Book = book1},
                    new AuthorBook{Book = book2}
                }
            };

            context.Add(author);

            await context.SaveChangesAsync();

            // Act
            var response = await controller.Get(1);

            // Assert
            var result = response.Value;
            Assert.AreEqual(expected: 1, actual: result!.Id);
            Assert.AreEqual(expected: 2, actual: result.Books.Count);
        }

        [TestMethod]
        public async Task Get_ShouldInvokeAuthorServiceGetMethod()
        {
            // Arrange
            var paginationDTO = new PaginationDTO(2, 3);

            // Act
            await controller.Get(paginationDTO);

            // Assert
            await authorService.Received(1).Get(paginationDTO);
        }


        [TestMethod]
        public async Task Post_ShouldCreateAuthor_WhenAuthorIsProvided()
        {
            // Arrange
            var context = BuildContext(nameDB);
            var newAuthor = new CreateAuthorDTO { FirstNames = "new", LastNames = "author" };

            // Act
            var response = await controller.Post(newAuthor);

            // Assert
            var result = response as CreatedAtRouteResult;
            Assert.IsNotNull(result);

            var context2 = BuildContext(nameDB);
            var count = await context2.Authors.CountAsync();
            Assert.AreEqual(expected: 1, actual: count);
        }

        [TestMethod]
        public async Task Put_Returns404_WhenAuthorDoesNotExist()
        {
            // Act
            var response = await controller.Put(createAuthorDTO: null!, 1);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: result!.StatusCode);
        }

        private const string container = "authors";
        private const string cache = "authors-get";

        [TestMethod]
        public async Task Put_UpdatesAuthor_WhenAuthorWithoutPhotoIsProvided()
        {
            // Arrange
            var context = BuildContext(nameDB);

            context.Authors.Add(new Author
            {
                FirstName = "Felipe",
                LastName = "Perez",
                Identification = "Id"
            });

            await context.SaveChangesAsync();

            var createAuthorDTO = new CreateAuthorWithPhotoDTO
            {
                FirstNames = "Felipe2",
                LastNames = "Perez2",
                Identification = "Id2"
            };

            // Act
            var response = await controller.Put(createAuthorDTO, 1);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(204, result!.StatusCode);

            var context3 = BuildContext(nameDB);
            var updatedAuthor = await context3.Authors.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", actual: updatedAuthor.FirstName);
            Assert.AreEqual(expected: "Perez2", actual: updatedAuthor.LastName);
            Assert.AreEqual(expected: "Id2", actual: updatedAuthor.Identification);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await fileStorage.DidNotReceiveWithAnyArgs().Edit(default, default!, default!);
        }

        [TestMethod]
        public async Task Put_UpdatesAuthor_WhenAuthorWithPhotoIsProvided()
        {
            // Arrange
            var context = BuildContext(nameDB);

            var previousUrl = "URL-1";
            var newUrl = "URL-2";
            fileStorage.Edit(default, default!, default!).ReturnsForAnyArgs(newUrl);

            context.Authors.Add(new Author
            {
                FirstName = "Felipe",
                LastName = "Perez",
                Identification = "Id",
                Photo = previousUrl
            });

            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var createAuthorDTO = new CreateAuthorWithPhotoDTO
            {
                FirstNames = "Felipe2",
                LastNames = "Perez2",
                Identification = "Id2",
                Photo = formFile
            };

            // Act
            var response = await controller.Put(createAuthorDTO, 1);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(204, result!.StatusCode);

            var context3 = BuildContext(nameDB);
            var updatedAuthor = await context3.Authors.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", actual: updatedAuthor.FirstName);
            Assert.AreEqual(expected: "Perez2", actual: updatedAuthor.LastName);
            Assert.AreEqual(expected: "Id2", actual: updatedAuthor.Identification);
            Assert.AreEqual(expected: newUrl, actual: updatedAuthor.Photo);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await fileStorage.Received(1).Edit(previousUrl, container, formFile);
        }

        [TestMethod]
        public async Task Patch_Returns400_WhenPatchDocIsNull()
        {
            // Act
            var response = await controller.Patch(1, patchDoc: null!);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(400, result!.StatusCode);
        }


        [TestMethod]
        public async Task Patch_Returns404_WhenAuthorDoesNotExist()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<AuthorPatchDTO>();

            // Act
            var response = await controller.Patch(1, patchDoc);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(404, result!.StatusCode);
        }

        [TestMethod]
        public async Task Patch_ReturnsValidationProblem_WhenValidationErrorOccurs()
        {
            // Arrange
            var context = BuildContext(nameDB);
            context.Authors.Add(new Author
            {
                FirstName = "Felipe",
                LastName = "Perez",
                Identification = "123"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var errorMessage = "error message";
            controller.ModelState.AddModelError("", errorMessage);

            var patchDoc = new JsonPatchDocument<AuthorPatchDTO>();

            // Act
            var response = await controller.Patch(1, patchDoc);

            // Assert
            var result = response as ObjectResult;
            var problemDetails = result!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: errorMessage, actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Patch_UpdateField_WhenOperationIsSent()
        {
            // Arrange
            var context = BuildContext(nameDB);
            context.Authors.Add(new Author
            {
                FirstName = "Felipe",
                LastName = "Perez",
                Identification = "123",
                Photo = "URL-1"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var patchDoc = new JsonPatchDocument<AuthorPatchDTO>();
            patchDoc.Operations.Add(new Operation<AuthorPatchDTO>("replace", "/FirstNames", null, "Felipe2"));

            // Act
            var response = await controller.Patch(1, patchDoc);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(expected: 204, result!.StatusCode);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);

            var context2 = BuildContext(nameDB);
            var authorDB = await context2.Authors.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", authorDB.FirstName);
            Assert.AreEqual(expected: "Perez", authorDB.LastName);
            Assert.AreEqual(expected: "123", authorDB.Identification);
            Assert.AreEqual(expected: "URL-1", authorDB.Photo);
        }

        [TestMethod]
        public async Task Delete_Returns404_WhenAuthorDoesNotExist()
        {
            // Act
            var response = await controller.Delete(1);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(404, result!.StatusCode);
        }

        [TestMethod]
        public async Task Delete_DeletesAuthor_WhenAuthorExists()
        {
            // Arrange
            var photoUrl = "URL-1";

            var context = BuildContext(nameDB);

            context.Authors.Add(new Author { FirstName = "Author1", LastName = "Author1", Photo = photoUrl });
            context.Authors.Add(new Author { FirstName = "Author2", LastName = "Author2" });

            await context.SaveChangesAsync();

            // Act
            var response = await controller.Delete(1);

            // Assert
            var result = response as StatusCodeResult;
            Assert.AreEqual(204, result!.StatusCode);

            var context2 = BuildContext(nameDB);
            var authorCount = await context2.Authors.CountAsync();
            Assert.AreEqual(expected: 1, actual: authorCount);

            var author2Exists = await context2.Authors.AnyAsync(x => x.FirstName == "Author2");
            Assert.IsTrue(author2Exists);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await fileStorage.Received(1).Delete(photoUrl, container);
        }
    }
}
