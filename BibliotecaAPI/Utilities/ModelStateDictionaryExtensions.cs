using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Utilities
{
    public static class ModelStateDictionaryExtensions
    {
        public static BadRequestObjectResult BuildProblemDetail(this ModelStateDictionary modelState)
        {
            var problemDetails = new ValidationProblemDetails(modelState)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            };

            return new BadRequestObjectResult(problemDetails);
        }
    }
}
