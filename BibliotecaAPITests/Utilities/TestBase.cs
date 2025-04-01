using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BibliotecaAPITests.Utilities
{
    public class TestBase
    {
        protected readonly JsonSerializerOptions jsonSerializerOptions
            = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        protected readonly Claim adminClaim = new Claim("isadmin", "1");

        protected ApplicationDbContext BuildContext(string nameDB)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(nameDB).Options;

            var dbContext = new ApplicationDbContext(options);
            return dbContext;
        }

        protected IMapper AutoMapperConfiguration()
        {
            var config = new MapperConfiguration(options =>
            {
                options.AddProfile(new AutoMapperProfiles());
            });

            return config.CreateMapper();
        }

        protected WebApplicationFactory<Program> BuildWebApplicationFactory(string nameDB,
            bool ignoreSecurity = true)
        {
            var factory = new WebApplicationFactory<Program>();

            factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    ServiceDescriptor descriptorDBContext = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))!;

                    if (descriptorDBContext is not null)
                    {
                        services.Remove(descriptorDBContext);
                    }

                    ServiceDescriptor descriptorOutputCache = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IOutputCacheStore))!;

                    if (descriptorOutputCache is not null)
                    {
                        services.Remove(descriptorOutputCache);
                        services.AddOutputCache();
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(nameDB));

                    if (ignoreSecurity)
                    {
                        services.AddSingleton<IAuthorizationHandler, AllowAnonymousHandler>();

                        services.AddControllers(options =>
                        {
                            options.Filters.Add(new FakeUserFilter());
                        });
                    }
                });
            });

            return factory;
        }

        protected async Task<string> CreateUser(string nameDB, WebApplicationFactory<Program> factory)
            => await CreateUser(nameDB, factory, [], "example@hotmail.com");

        protected async Task<string> CreateUser(string nameDB, WebApplicationFactory<Program> factory,
            IEnumerable<Claim> claims)
            => await CreateUser(nameDB, factory, claims, "example@hotmail.com");

        protected async Task<string> CreateUser(string nameDB, WebApplicationFactory<Program> factory,
            IEnumerable<Claim> claims, string email)
        {
            var registerUrl = "/api/users/register";
            string token = string.Empty;
            token = await GetToken(email, registerUrl, factory);

            if (claims.Any())
            {
                var context = BuildContext(nameDB);
                var user = await context.Users.Where(x => x.Email == email).FirstAsync();
                Assert.IsNotNull(user);

                var userClaims = claims.Select(x => new IdentityUserClaim<string>
                {
                    UserId = user.Id,
                    ClaimType = x.Type,
                    ClaimValue = x.Value
                });

                context.UserClaims.AddRange(userClaims);
                await context.SaveChangesAsync();
                var loginUrl = "/api/users/login";
                token = await GetToken(email, loginUrl, factory);
            }

            return token;
        }

        private async Task<string> GetToken(string email, string url,
            WebApplicationFactory<Program> factory)
        {
            var password = "aA123456!";
            var credentials = new UserCredentialsDTO { Email = email, Password = password };
            var client = factory.CreateClient();
            var response = await client.PostAsJsonAsync(url, credentials);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<AuthenticationResponseDTO>(content, jsonSerializerOptions)!;

            Assert.IsNotNull(authResponse.Token);

            return authResponse.Token;
        }
    }
}
