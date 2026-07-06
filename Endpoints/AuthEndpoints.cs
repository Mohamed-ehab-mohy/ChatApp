using ChatApp.DTOs.Auth;
using ChatApp.Models;
using ChatApp.Services;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace ChatApp.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<AppUser> userManager,
            TokenService tokenService,
            IValidator<RegisterRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var user = new AppUser { UserName = request.Email, Email = request.Email };
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            var token = tokenService.GenerateToken(user);
            return Results.Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                UserId = user.Id
            });
        }).AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            TokenService tokenService,
            IValidator<LoginRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Unauthorized();

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                return Results.Unauthorized();

            var token = tokenService.GenerateToken(user);
            return Results.Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                UserId = user.Id
            });
        }).AllowAnonymous();
    }
}
