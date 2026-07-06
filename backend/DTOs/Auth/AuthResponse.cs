namespace ChatApp.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}
