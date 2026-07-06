using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Services;

public class TokenService
{
    private readonly JwtSettings _settings;
    private readonly AppDbContext _db;

    public TokenService(JwtSettings settings, AppDbContext db)
    {
        _settings = settings;
        _db = db;
    }

    public string GenerateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(AppUser user)
    {
        var rt = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();

        return rt.Token;
    }

    public async Task<RefreshToken?> ValidateRefreshToken(string token)
    {
        return await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token && rt.RevokedAt == null && DateTime.UtcNow < rt.ExpiresAt);
    }

    public async Task RevokeRefreshToken(RefreshToken rt)
    {
        rt.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
