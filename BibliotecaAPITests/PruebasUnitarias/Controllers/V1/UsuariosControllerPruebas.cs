﻿using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using BibliotecaAPITests.Utilidades;
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

namespace BibliotecaAPITests.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class UsuariosControllerPruebas : BasePruebas
    {
        private string nombreBD = Guid.NewGuid().ToString();
        private UserManager<Usuario> userManager = null!;
        private SignInManager<Usuario> signInManager = null!;
        private UsuariosController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = ConstruirContext(nombreBD);
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            var miConfiguracion = new Dictionary<string, string>
            {
                {
                    "llavejwt", "askjdansjkdansjkdNJKANSDJKANSDJKASNDAJKSNDJ"
                }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(miConfiguracion!)
                .Build();

            var contextAccessor = Substitute.For<IHttpContextAccessor>();
            var userClaimsFactory = Substitute.For<IUserClaimsPrincipalFactory<Usuario>>();

            signInManager = Substitute.For<SignInManager<Usuario>>(userManager,
                contextAccessor, userClaimsFactory, null, null, null, null);

            var servicioUsuarios = Substitute.For<IServiciosUsuarios>();

            var mapper = ConfigurarAutoMapper();

            controller = new UsuariosController(context, mapper, userManager, configuration,
                signInManager, servicioUsuarios);
        }

        [TestMethod]
        public async Task Registrar_DevuelveValidationProblem_CuandoNoEsExitoso()
        {
            // Arrange
            var mensajeDeError = "prueba";
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                    .Returns(IdentityResult.Failed(new IdentityError
                    {
                        Code = "prueba",
                        Description = mensajeDeError
                    }));

            // Act
            var respuesta = await controller.Registrar(credenciales);

            // Assert
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Registrar_DevuelveToken_CuandoEsExitoso()
        {
            // Arrange
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                    .Returns(IdentityResult.Success);

            // Act
            var respuesta = await controller.Registrar(credenciales);

            // Assert
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblem_CuandoUsuarioNoExiste()
        {
            // Arrange
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(null!));

            // Act
            var respuesta = await controller.Login(credenciales);

            // Assert
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto",
                actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblem_CuandoLoginEsIncorrecto()
        {
            // Arrange
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            var usuario = new Usuario { Email = credenciales.Email };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(usuario));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false)
                .Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var respuesta = await controller.Login(credenciales);

            // Assert
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto",
                actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveToken_CuandoLoginEsCorrecto()
        {
            // Arrange
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            var usuario = new Usuario { Email = credenciales.Email };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(usuario));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false)
                .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var respuesta = await controller.Login(credenciales);

            // Assert
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }
    }
}
