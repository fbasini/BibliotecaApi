using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilities
{
    public static class HttpContextExtensions
    {
        public async static Task InsertPaginationParamsInHeader<T>(this HttpContext httpContext, IQueryable<T> queryable)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            double totalCount = await queryable.CountAsync();
            httpContext.Response.Headers.Append("total-records-count", totalCount.ToString());
        }
    }
}
