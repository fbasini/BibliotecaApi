using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaAPI.Services
{
    public class LinkGenerator : ILinkGenerator
    {
        private readonly Microsoft.AspNetCore.Routing.LinkGenerator linkGenerator;
        private readonly IAuthorizationService authorizationService;
        private readonly IHttpContextAccessor httpContextAccessor;

        public LinkGenerator(Microsoft.AspNetCore.Routing.LinkGenerator linkGenerator,
            IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            this.linkGenerator = linkGenerator;
            this.authorizationService = authorizationService;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResourcesListDTO<AuthorDTO>>
            GenerateLinks(List<AuthorDTO> authors)
        {
            var result = new ResourcesListDTO<AuthorDTO> { Values = authors };

            var user = httpContextAccessor.HttpContext!.User;
            var isAdmin = await authorizationService.AuthorizeAsync(user, "isadmin");

            foreach (var dto in authors)
            {
                GenerateLinks(dto, isAdmin.Succeeded);
            }

            result.Links.Add(new HATEOASDataDTO(
                Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                "GetAuthors", new { })!,
                Description: "self",
                Method: "GET"
            ));

            if (isAdmin.Succeeded)
            {
                result.Links.Add(new HATEOASDataDTO(
                     Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                     "CreateAuthor", new { })!,
                     Description: "author-create",
                     Method: "POST"
                ));

                result.Links.Add(new HATEOASDataDTO(
                     Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                     "CreateAuthorWithPhoto", new { })!,
                     Description: "author-create-with-photo",
                     Method: "POST"
                ));
            }

            return result;
        }

        public async Task GenerateLinks(AuthorDTO authorDTO)
        {
            var user = httpContextAccessor.HttpContext!.User;
            var isAdmin = await authorizationService.AuthorizeAsync(user, "isadmin");
            GenerateLinks(authorDTO, isAdmin.Succeeded);
        }

        private void GenerateLinks(AuthorDTO authorDTO, bool isAdmin)
        {
            authorDTO.Links.Add(new HATEOASDataDTO(
                Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                "GetAuthor", new { id = authorDTO.Id })!,
                Description: "self",
                Method: "GET"
            ));

            if (isAdmin)
            {
                authorDTO.Links.Add(new HATEOASDataDTO(
                    Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                    "UpdateAuthor", new { id = authorDTO.Id })!,
                    Description: "author-update",
                    Method: "PUT"
                ));

                authorDTO.Links.Add(new HATEOASDataDTO(
                    Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                    "PatchAuthor", new { id = authorDTO.Id })!,
                    Description: "author-patch",
                    Method: "PATCH"
                ));

                authorDTO.Links.Add(new HATEOASDataDTO(
                    Link: linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext!,
                    "DeleteAuthor", new { id = authorDTO.Id })!,
                    Description: "author-delete",
                    Method: "DELETE"
                ));
            }
        }
    }
}
