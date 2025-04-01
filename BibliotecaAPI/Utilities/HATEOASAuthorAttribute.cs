using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using BibliotecaAPI.Services;

namespace BibliotecaAPI.Utilities
{
    public class HATEOASAuthorAttribute : HATEOASFilterAttribute
    {
        private readonly ILinkGenerator linkGenerator;

        public HATEOASAuthorAttribute(ILinkGenerator linkGenerator)
        {
            this.linkGenerator = linkGenerator;
        }

        public override async Task OnResultExecutionAsync
            (ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var includeHATEOAS = ShouldIncludeHATEOAS(context);

            if (!includeHATEOAS)
            {
                await next();
                return;
            }

            var result = context.Result as ObjectResult;
            var model = result!.Value as AuthorDTO ??
                    throw new ArgumentNullException("Expected an instance of AuthorDTO");
            await linkGenerator.GenerateLinks(model);
            await next();
        }
    }
}
