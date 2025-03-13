using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPITests.Utilidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BibliotecaAPITests.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        private static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();


        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste()
        {
            // Arrange
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Act
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Assert
            var statusCode = respuesta.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste()
        {
            // Arrange
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor() { Nombres = "Felipe", Apellidos = "Perez" });
            context.Autores.Add(new Autor() { Nombres = "Juan", Apellidos = "Gomez" });
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Act
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Assert
            respuesta.EnsureSuccessStatusCode();

            var autor = JsonSerializer.Deserialize<AutorConLibrosDTO>(
                await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            Assert.AreEqual(expected: 1, autor.Id);
        }

        [TestMethod]
        public async Task Post_Devuelve401_CuandoUsuarioNoEstaAutenticado()
        {
            // Arrange
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var cliente = factory.CreateClient();
            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Perez",
                Identificacion = "123"
            };

            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve403_CuandoUsuarioNoEsAdmin()
        {
            // Arrange
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var token = await CrearUsuario(nombreBD, factory);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Perez",
                Identificacion = "123"
            };

            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve201_CuandoUsuarioEsAdmin()
        {
            // Arrange
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var claims = new List<Claim> { adminClaim };
            var token = await CrearUsuario(nombreBD, factory, claims);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Perez",
                Identificacion = "123"
            };

            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Assert
            respuesta.EnsureSuccessStatusCode();
            Assert.AreEqual(expected: HttpStatusCode.Created, actual: respuesta.StatusCode);
        }
    }
}
