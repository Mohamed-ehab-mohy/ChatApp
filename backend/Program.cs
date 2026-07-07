using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using ChatApp.Data;
using ChatApp.Endpoints;
using ChatApp.Hubs;
using ChatApp.Models;
using ChatApp.Services;
using ChatApp.Settings;
using FluentValidation;
using Ganss.Xss;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

string NormalizeConnectionString(string? connStr)
{
    if (string.IsNullOrEmpty(connStr))
        throw new InvalidOperationException("Connection string is not configured. Set ConnectionStrings:DefaultConnection or DATABASE_URL.");

    if (connStr.StartsWith("postgresql://") || connStr.StartsWith("postgres://"))
    {
        var u = new Uri(connStr);
        var userInfo = u.UserInfo?.Split(':') ?? [];
        var host = u.Host;
        var port = u.IsDefaultPort ? "5432" : u.Port.ToString();
        var db = u.AbsolutePath.TrimStart('/');
        var user = userInfo.Length > 0 ? userInfo[0] : "";
        var pass = userInfo.Length > 1 ? userInfo[1] : "";
        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }

    return connStr;
}

var connStr = NormalizeConnectionString(
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection"));

Console.WriteLine($"[ChatApp] Database: {(connStr.Contains("railway") ? "Railway (cloud)" : "Local")}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
})
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddSignalR();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Api-Version")
    );
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PushNotificationService>();

var vapidSettings = builder.Configuration.GetSection("VapidSettings").Get<VapidSettings>()!;
builder.Services.AddSingleton(vapidSettings);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "ChatApp API",
            Description = "Real-time chat application with SignalR",
            Version = "v1"
        };

        var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token"
        };
        document.Components ??= new();
        if (document.Components.SecuritySchemes is null)
            document.Components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        return Task.CompletedTask;
    });
});

builder.Services.AddSingleton<HtmlSanitizer>(_ =>
{
    var s = new HtmlSanitizer();
    s.AllowedTags.Add("b"); s.AllowedTags.Add("i");
    s.AllowedTags.Add("u"); s.AllowedTags.Add("a");
    s.AllowedAttributes.Add("href");
    s.AllowedSchemes.Add("https");
    return s;
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        var msg = app.Environment.IsDevelopment() ? ex.Message : "An error occurred";
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new { errors = new[] { msg } }));
    }
});

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("ChatApp API");
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/hub") && !context.Request.Path.StartsWithSegments("/scalar") && !context.Request.Path.StartsWithSegments("/openapi"))
    {
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";
    }
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseRateLimiter();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1.0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("/api/v1").WithApiVersionSet(versionSet);
var authGroup = v1.MapGroup("/auth").RequireRateLimiting("AuthPolicy");
var messageGroup = v1.MapGroup("/messages").RequireAuthorization();

var notificationGroup = v1.MapGroup("/notifications").RequireAuthorization();

authGroup.MapAuthEndpoints();
messageGroup.MapMessageEndpoints();
notificationGroup.MapNotificationEndpoints();

app.MapHub<ChatHub>("/hub/chat");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
