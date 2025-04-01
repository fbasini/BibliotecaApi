using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BibliotecaAPI.Utilities
{
    public class HATEOASFilterAttribute : ResultFilterAttribute
    {
        protected bool ShouldIncludeHATEOAS(ResultExecutingContext context)
        {
            if (context.Result is not ObjectResult result || !IsSuccessfulResponse(result))
            {
                return false;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue("IncludeHATEOAS", out var header))
            {
                return false;
            }

            return string.Equals(header, "Y", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSuccessfulResponse(ObjectResult result)
        {
            if (result.Value is null)
            {
                return false;
            }

            if (result.StatusCode.HasValue && !result.StatusCode.Value.ToString().StartsWith("2"))
            {
                return false;
            }

            return true;
        }
    }
}
