using ChatApp.DTOs.Auth;
using ChatApp.Models;
using ChatApp.Services;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace ChatApp.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshCookieName = "refresh_token";

    private static void SetRefreshCookie(HttpContext context, string token)
    {
        context.Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.FromDays(7)
        });
    }

    private static void ClearRefreshCookie(HttpContext context)
    {
        context.Response.Cookies.Append(RefreshCookieName, "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.Zero,
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        });
    }

    public static void MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            RegisterRequest request,
            HttpContext context,
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
            var refreshToken = await tokenService.GenerateRefreshToken(user);
            SetRefreshCookie(context, refreshToken);
            return Results.Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                UserId = user.Id
            });
        }).AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            HttpContext context,
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
            var refreshToken = await tokenService.GenerateRefreshToken(user);
            SetRefreshCookie(context, refreshToken);
            return Results.Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                UserId = user.Id
            });
        }).AllowAnonymous();

        group.MapPost("/refresh", async (
            HttpContext context,
            TokenService tokenService,
            UserManager<AppUser> userManager) =>
        {
            if (!context.Request.Cookies.TryGetValue(RefreshCookieName, out var rt) || string.IsNullOrWhiteSpace(rt))
                return Results.Unauthorized();

            var stored = await tokenService.ValidateRefreshToken(rt);
            if (stored is null)
            {
                ClearRefreshCookie(context);
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(stored.UserId);
            if (user is null)
            {
                ClearRefreshCookie(context);
                return Results.Unauthorized();
            }

            await tokenService.RevokeRefreshToken(stored);

            var newToken = tokenService.GenerateToken(user);
            var newRefreshToken = await tokenService.GenerateRefreshToken(user);
            SetRefreshCookie(context, newRefreshToken);

            return Results.Ok(new AuthResponse
            {
                Token = newToken,
                Email = user.Email!,
                UserId = user.Id
            });
        }).AllowAnonymous();

        group.MapPost("/logout", async (
            HttpContext context,
            TokenService tokenService) =>
        {
            if (context.Request.Cookies.TryGetValue(RefreshCookieName, out var rt) && !string.IsNullOrWhiteSpace(rt))
            {
                var stored = await tokenService.ValidateRefreshToken(rt);
                if (stored is not null)
                    await tokenService.RevokeRefreshToken(stored);
            }
            ClearRefreshCookie(context);
            return Results.Ok(new { message = "Logged out" });
        }).AllowAnonymous();
    }
}
