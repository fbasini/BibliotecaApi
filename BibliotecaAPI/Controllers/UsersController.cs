﻿using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly UserManager<User> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<User> signInManager;
        private readonly IUserService userService;

        public UsersController(ApplicationDbContext applicationDbContext, IMapper mapper,
            UserManager<User> userManager, IConfiguration configuration, SignInManager<User> signInManager,
            IUserService userService)
        {
            context = applicationDbContext;
            this.mapper = mapper;
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
            this.userService = userService;
        }

        [HttpGet(Name = "GetUsers")]
        [Authorize(Policy = "isadmin")]
        [EndpointSummary("Retrieves all users")]
        [SwaggerResponse(200, "List of users retrieved successfully.", typeof(IEnumerable<UserDTO>))]
        [SwaggerResponse(403, "Access denied. Admin role required.")]
        public async Task<IEnumerable<UserDTO>> Get()
        {
            var users = await context.Users.ToListAsync();
            var usersDTO = mapper.Map<IEnumerable<UserDTO>>(users);
            return usersDTO;
        }

        [HttpPost("register", Name = "RegisterUser")]
        [EndpointSummary("Registers a new user")]
        [SwaggerResponse(201, "User registered successfully.", typeof(AuthenticationResponseDTO))]
        [SwaggerResponse(400, "Invalid request.")]
        public async Task<ActionResult<AuthenticationResponseDTO>> Register(
            UserCredentialsDTO userCredentialsDTO)
        {
            var user = new User
            {
                UserName = userCredentialsDTO.Email,
                Email = userCredentialsDTO.Email
            };

            var result = await userManager.CreateAsync(user, userCredentialsDTO.Password!);

            if (result.Succeeded)
            {
                var authResponse = await BuildToken(userCredentialsDTO);
                return authResponse;
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return ValidationProblem();
            }
        }

        [HttpPost("login", Name = "LoginUser")]
        [EndpointSummary("Login")]
        [SwaggerResponse(200, "User logged in successfully.", typeof(AuthenticationResponseDTO))]
        [SwaggerResponse(400, "Invalid login.")]
        public async Task<ActionResult<AuthenticationResponseDTO>> Login(
            UserCredentialsDTO userCredentialsDTO)
        {
            var user = await userManager.FindByEmailAsync(userCredentialsDTO.Email);

            if (user is null)
            {
                return ReturnInvalidLogin();
            }

            var result = await signInManager.CheckPasswordSignInAsync(user,
                userCredentialsDTO.Password!, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return await BuildToken(userCredentialsDTO);
            }
            else
            {
                return ReturnInvalidLogin();
            }
        }

        [HttpPut(Name = "UpdateUser")]
        [Authorize]
        [EndpointSummary("Updates a user")]
        [SwaggerResponse(204, "User updated successfully.")]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(401, "Unauthorized access.")]
        public async Task<ActionResult> Put(UpdateUserDTO updateUserDTO)
        {
            var user = await userService.GetUser();

            if (user is null)
            {
                return NotFound();
            }

            user.BirthDate = updateUserDTO.BirthDate;

            await userManager.UpdateAsync(user);
            return NoContent();
        }

        [HttpGet("renew-token", Name = "RenewToken")]
        [Authorize]
        [EndpointSummary("Refresh the token")]
        [SwaggerResponse(200, "Token refreshed successfully.", typeof(AuthenticationResponseDTO))]
        [SwaggerResponse(401, "Refresh token is invalid or expired.")]
        public async Task<ActionResult<AuthenticationResponseDTO>> RenewToken()
        {
            var user = await userService.GetUser();

            if (user is null)
            {
                return NotFound();
            }

            var userCredentialsDTO = new UserCredentialsDTO { Email = user.Email! };

            var authResponse = await BuildToken(userCredentialsDTO);
            return authResponse;
        }

        [HttpPost("make-admin")]
        [Authorize(Policy = "isadmin")]
        [EndpointSummary("Grants admin role to a user")]
        [SwaggerResponse(204, "Role assigned successfully.")]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(403, "Access denied. Admin role required.")]
        public async Task<ActionResult> MakeAdmin(EditClaimDTO editClaimDTO)
        {
            var user = await userManager.FindByEmailAsync(editClaimDTO.Email);

            if (user is null)
            {
                return NotFound();
            }

            await userManager.AddClaimAsync(user, new Claim("isadmin", "true"));
            return NoContent();
        }

        [HttpPost("remove-admin")]
        [Authorize(Policy = "isadmin")]
        [EndpointSummary("Removes admin role from a user")]
        [SwaggerResponse(204, "Role removed successfully.")]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(403, "Access denied. Admin role required.")]
        public async Task<ActionResult> RemoveAdmin(EditClaimDTO editClaimDTO)
        {
            var user = await userManager.FindByEmailAsync(editClaimDTO.Email);

            if (user is null)
            {
                return NotFound();
            }

            await userManager.RemoveClaimAsync(user, new Claim("isadmin", "true"));
            return NoContent();
        }

        private ActionResult ReturnInvalidLogin()
        {
            ModelState.AddModelError(string.Empty, "Invalid login");
            return ValidationProblem();
        }

        private async Task<AuthenticationResponseDTO> BuildToken(
            UserCredentialsDTO userCredentialsDTO)
        {
            var claims = new List<Claim>
            {
                new Claim("email", userCredentialsDTO.Email)
            };

            var user = await userManager.FindByEmailAsync(userCredentialsDTO.Email);
            var claimsDB = await userManager.GetClaimsAsync(user!);

            claims.AddRange(claimsDB);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwtkey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddYears(1);

            var securityToken = new JwtSecurityToken(issuer: null, audience: null,
                claims: claims, expires: expiration, signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new AuthenticationResponseDTO
            {
                Token = token,
                Expiration = expiration
            };
        }
    }
}
