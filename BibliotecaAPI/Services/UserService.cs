using BibliotecaAPI.Entities;
using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> userManager;
        private readonly IHttpContextAccessor contextAccessor;

        public UserService(UserManager<User> userManager, IHttpContextAccessor contextAccessor)
        {
            this.userManager = userManager;
            this.contextAccessor = contextAccessor;
        }

        public async Task<User?> GetUser()
        {
            var emailClaim = contextAccessor.HttpContext!
                                .User.Claims.Where(x => x.Type == "email").FirstOrDefault();

            if (emailClaim is null)
            {
                return null;
            }

            var email = emailClaim.Value;
            return await userManager.FindByEmailAsync(email);

        }
    }
}
