using BibliotecaAPI.Controllers;
using BibliotecaAPI.DTOs;
using BibliotecaAPITests.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.UnitTesting.Controllers
{
    [TestClass]
    public class BooksControllerTests : TestBase
    {
        [TestMethod]
        public async Task Get_ReturnsZeroBooks_WhenThereAreNotBooks()
        {
            // Arrange
            var nameDB = Guid.NewGuid().ToString();
            var context = BuildContext(nameDB);
            var mapper = AutoMapperConfiguration();
            IOutputCacheStore outputCacheStore = null!;

            var controller = new BooksController(context, mapper, outputCacheStore);

            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var paginationDTO = new PaginationDTO(1, 1);

            // Act
            var response = await controller.Get(paginationDTO);

            // Assert
            Assert.AreEqual(expected: 0, actual: response.Count());
        }
    }
}
