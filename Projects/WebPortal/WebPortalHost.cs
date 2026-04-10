using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Server;
using Server.WebPortal.Configuration;
using Server.WebPortal.Endpoints;
using Server.WebPortal.Middleware;
using Server.WebPortal.Services;

namespace Server.WebPortal;

public static class WebPortalHost
{
    private static readonly Server.Logging.ILogger logger = Server.Logging.LogFactory.GetLogger(typeof(WebPortalHost));
    private static WebApplication? _app;

    public static void Configure()
    {
        WebPortalConfiguration.Configure();
    }

    public static void Initialize()
    {
        if (!WebPortalConfiguration.Enabled)
        {
            logger.Information("Web Portal is disabled");
            return;
        }

        // Start Kestrel on a background thread - it has its own thread pool
        _ = Task.Run(StartWebServer);
    }

    private static async Task StartWebServer()
    {
        try
        {
            var wwwrootPath = Path.Combine(Core.BaseDirectory, "wwwroot");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Core.BaseDirectory,
                WebRootPath = wwwrootPath
            });

            // Configure Kestrel
            builder.WebHost.UseUrls($"http://0.0.0.0:{WebPortalConfiguration.Port}");

            // Suppress default console logging to avoid cluttering game server output
            builder.Logging.ClearProviders();

            // Add services
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddSingleton<AccountLockoutService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<AccountService>();

            // Configure JWT authentication
            var key = Encoding.UTF8.GetBytes(WebPortalConfiguration.JwtSecret);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = false,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidIssuer = "ModernUO",
                        ValidateAudience = false,
                        ValidAudience = "ModernUO-WebPortal",
                        ValidateLifetime = false,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RequireSignedTokens = false
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Cookies["access_token"];
                            if (string.IsNullOrEmpty(token))
                            {
                                var authHeader = context.Request.Headers["Authorization"].ToString();
                                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    token = authHeader.Substring("Bearer ".Length).Trim();
                                }
                            }

                            Console.WriteLine($"[JWT OnMessageReceived] Cookie: {context.Request.Cookies["access_token"]?.Substring(0, Math.Min(30, context.Request.Cookies["access_token"]?.Length ?? 0))}...");
                            Console.WriteLine($"[JWT OnMessageReceived] Header: {context.Request.Headers["Authorization"]}");
                            Console.WriteLine($"[JWT OnMessageReceived] Token set: {!string.IsNullOrEmpty(token)}");

                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"[JWT FAILED] {context.Exception.GetType().Name}: {context.Exception.Message}");
                            if (context.Exception.InnerException != null)
                            {
                                Console.WriteLine($"[JWT FAILED] Inner: {context.Exception.InnerException.Message}");
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            Console.WriteLine($"[JWT CHALLENGE] AuthFailure: {context.AuthenticateFailure?.Message}");
                            Console.WriteLine($"[JWT CHALLENGE] Error: {context.Error}");
                            Console.WriteLine($"[JWT CHALLENGE] ErrorDescription: {context.ErrorDescription}");
                            Console.WriteLine($"[JWT CHALLENGE] Stack: {context.AuthenticateFailure?.StackTrace?.Substring(0, Math.Min(500, context.AuthenticateFailure?.StackTrace?.Length ?? 0))}");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Security middleware - order matters
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<RateLimitingMiddleware>();

            // Static files (frontend) - serve before auth so public pages work
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Auth
            app.UseAuthentication();
            app.UseAuthorization();

            // API endpoints
            app.MapAuthEndpoints();
            app.MapAccountEndpoints();
            app.MapServerEndpoints();

            // Debug endpoint to inspect JWT config
            app.MapGet("/debug/jwt", async (HttpContext ctx) =>
            {
                var cookie = ctx.Request.Cookies["access_token"];
                var authHeader = ctx.Request.Headers["Authorization"].ToString();
                var user = ctx.User?.Identity;
                var identity = ctx.User?.Identity as System.Security.Claims.ClaimsIdentity;
                var claims = identity?.Claims.Select(c => $"{c.Type}={c.Value}").ToArray();
                return Results.Ok(new
                {
                    CookieLength = cookie?.Length ?? 0,
                    HasAuthHeader = !string.IsNullOrEmpty(authHeader),
                    AuthHeader = authHeader,
                    UserIdentity = user?.Name,
                    IsAuthenticated = user?.IsAuthenticated,
                    Claims = claims
                });
            });

            _app = app;

            logger.Information("Web Portal starting on port {Port}", WebPortalConfiguration.Port);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Web Portal failed to start");
        }
    }
}
