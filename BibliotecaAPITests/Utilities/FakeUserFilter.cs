using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BibliotecaAPITests.Utilities
{
    public class FakeUserFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Antes de la accion
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("email", "example@hotmail.com"),
            }, "test"));

            await next();

            // Despues de la accion
        }
    }
}
