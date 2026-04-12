using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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

            // Configure forwarded headers when behind a reverse proxy (Cloudflare, nginx, etc.)
            // This must be registered BEFORE building the app so the middleware pipeline is correct.
            if (WebPortalConfiguration.BehindReverseProxy)
            {
                builder.Services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                        | ForwardedHeaders.XForwardedProto
                        | ForwardedHeaders.XForwardedHost;

                    // Trust all proxies — Cloudflare IPs change frequently and the server
                    // is not directly exposed to the internet, so spoofing is not a concern.
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }

            // Add services
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddSingleton<AccountLockoutService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<AccountService>();
            builder.Services.AddSingleton<EmailService>();

            // Configure JWT authentication
            var key = Encoding.UTF8.GetBytes(WebPortalConfiguration.JwtSecret);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = "ModernUO",
                        ValidateAudience = true,
                        ValidAudience = "ModernUO-WebPortal",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RequireSignedTokens = true,
                        NameClaimType = JwtRegisteredClaimNames.Sub
                    };

                    // Disable inbound claim mapping so "sub" stays as "sub" instead of being
                    // mapped to the long URI ClaimTypes.NameIdentifier. This ensures
                    // context.User.Identity.Name returns the username via NameClaimType above.
                    options.TokenHandlers.Clear();
                    options.TokenHandlers.Add(new JwtSecurityTokenHandler { MapInboundClaims = false });

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Read token from HttpOnly cookie first, fall back to Authorization header
                            var token = context.Request.Cookies["access_token"];
                            if (string.IsNullOrEmpty(token))
                            {
                                var authHeader = context.Request.Headers["Authorization"].ToString();
                                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    token = authHeader.Substring("Bearer ".Length).Trim();
                                }
                            }

                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Forwarded headers must be first so all downstream middleware sees real client IPs
            if (WebPortalConfiguration.BehindReverseProxy)
            {
                app.UseForwardedHeaders();
            }

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
