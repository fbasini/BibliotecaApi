using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using BibliotecaAPI.Services.V1;

namespace BibliotecaAPI.Utilities.V1
{
    public class HATEOASAutorAttribute : HATEOASFilterAttribute
    {
        private readonly IGeneradorEnlaces generadorEnlaces;

        public HATEOASAutorAttribute(IGeneradorEnlaces generadorEnlaces)
        {
            this.generadorEnlaces = generadorEnlaces;
        }

        public override async Task OnResultExecutionAsync
            (ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var incluirHATEOAS = DebeIncluirHATEOAS(context);

            if (!incluirHATEOAS)
            {
                await next();
                return;
            }

            var result = context.Result as ObjectResult;
            var modelo = result!.Value as AutorDTO ??
                    throw new ArgumentNullException("Se esperaba una instancia de AutorDTO");
            await generadorEnlaces.GenerarEnlaces(modelo);
            await next();
        }
    }
}
