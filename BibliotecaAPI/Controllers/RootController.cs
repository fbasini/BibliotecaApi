using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/root")]
    [Authorize]
    public class RootController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;

        public RootController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet(Name = "GetRoot")]
        [AllowAnonymous]
        public async Task<IEnumerable<HATEOASDataDTO>> Get()
        {
            var hateoasData = new List<HATEOASDataDTO>();

            var isAdmin = await authorizationService.AuthorizeAsync(User, "isadmin");

            hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("GetRoot", new { })!,
                Description: "self", Method: "GET"));

            hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("GetAuthors", new { })!,
                Description: "authors-get", Method: "GET"));

            hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("RegisterUser", new { })!,
                Description: "user-register", Method: "POST"));

            hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("LoginUser", new { })!,
                Description: "user-login", Method: "POST"));


            if (User.Identity!.IsAuthenticated)
            {
                hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("UpdateUser", new { })!,
                    Description: "user-update", Method: "PUT"));

                hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("RenewToken", new { })!,
                    Description: "token-renew", Method: "GET"));
            }

            if (isAdmin.Succeeded)
            {
                hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("CreateAuthor", new { })!,
                    Description: "author-create", Method: "POST"));

                hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("CreateAuthors", new { })!,
                    Description: "authors-create", Method: "POST"));

                hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("CreateBook", new { })!,
                    Description: "book-create", Method: "POST"));

                hateoasData.Add(new HATEOASDataDTO(Link: Url.Link("GetUsers", new { })!,
                    Description: "users-get", Method: "GET"));
            }

            return hateoasData;
        }
    }
}
