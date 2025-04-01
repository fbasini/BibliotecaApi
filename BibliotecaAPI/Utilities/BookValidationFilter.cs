using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilities
{
    public class BookValidationFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext dbContext;

        public BookValidationFilter(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ActionArguments.TryGetValue("createBookDTO", out var value) ||
                    value is not CreateBookDTO createBookDTO)
            {
                context.ModelState.AddModelError(string.Empty, "The submitted model is not valid");
                context.Result = context.ModelState.BuildProblemDetail();
                return;
            }

            if (createBookDTO.AuthorsIds is null || createBookDTO.AuthorsIds.Count == 0)
            {
                context.ModelState.AddModelError(nameof(createBookDTO.AuthorsIds),
                    "A book cannot be created without authors");
                context.Result = context.ModelState.BuildProblemDetail();
                return;
            }

            var existingAuthorIds = await dbContext.Authors
                                    .Where(x => createBookDTO.AuthorsIds.Contains(x.Id))
                                    .Select(x => x.Id).ToListAsync();

            if (existingAuthorIds.Count != createBookDTO.AuthorsIds.Count)
            {
                var nonExistingAuthors = createBookDTO.AuthorsIds.Except(existingAuthorIds);
                var nonExitingAuthorsString = string.Join(",", nonExistingAuthors);
                var errorMessage = $"The following authors do not exis: {nonExitingAuthorsString}";
                context.ModelState.AddModelError(nameof(createBookDTO.AuthorsIds),
                    errorMessage);
                context.Result = context.ModelState.BuildProblemDetail();
                return;
            }

            await next();
        }
    }
}
