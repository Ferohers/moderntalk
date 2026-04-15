using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Server;
using Server.CommanderApi.Configuration;
using Server.CommanderApi.Endpoints;
using Server.CommanderApi.Middleware;
using Server.CommanderApi.Services;

namespace Server.CommanderApi;

public static class CommanderApiHost
{
    private static readonly Server.Logging.ILogger logger = Server.Logging.LogFactory.GetLogger(typeof(CommanderApiHost));
    private static WebApplication? _app;

    public static void Configure()
    {
        CommanderApiConfiguration.Configure();
    }

    public static void Initialize()
    {
        if (!CommanderApiConfiguration.Enabled)
        {
            logger.Information("Commander API is disabled");
            return;
        }

        // Start Kestrel on a background thread — it has its own thread pool
        _ = Task.Run(StartWebServer);
    }

    private static async Task StartWebServer()
    {
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Core.BaseDirectory
            });

            // Configure Kestrel on the Commander API port (default: 8090)
            builder.WebHost.UseUrls($"http://0.0.0.0:{CommanderApiConfiguration.Port}");

            // Add console logger for diagnostics (errors will show in container logs)
            // Suppress noisy ASP.NET Core infrastructure logs (routing, hosting, results)
            // that fire on every request — only warnings and errors from those are useful.
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

            // Register services
            builder.Services.AddSingleton<AdminAuthService>();
            builder.Services.AddSingleton<ServerService>();
            builder.Services.AddSingleton<PlayerService>();
            builder.Services.AddSingleton<AccountService>();
            builder.Services.AddSingleton<AuditLogService>();

            // Register custom route constraint for 'uint' (not built-in to ASP.NET Core)
            builder.Services.AddRouting(options =>
            {
                options.ConstraintMap.Add("uint", typeof(UIntRouteConstraint));
            });

            // Configure JWT authentication
            var key = Encoding.UTF8.GetBytes(CommanderApiConfiguration.JwtSecret);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = "ModernUO-CommanderApi",
                        ValidateAudience = true,
                        ValidAudience = "ModernUO-CommanderApi-Client",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RequireSignedTokens = true,
                        NameClaimType = JwtRegisteredClaimNames.Sub
                    };

                    // Disable inbound claim mapping so "sub" stays as "sub"
                    options.TokenHandlers.Clear();
                    options.TokenHandlers.Add(new JwtSecurityTokenHandler { MapInboundClaims = false });

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Read token from Authorization header only (native app, not browser)
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            logger.Warning("Commander API: JWT authentication failed: {Error}", context.Exception?.Message);
                            return Task.CompletedTask;
                        }
                    };
                });

            // Default policy: ALL endpoints require auth unless explicitly marked [AllowAnonymous]
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Configure CORS
            var corsOrigins = CommanderApiConfiguration.GetCorsOriginArray();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    if (corsOrigins.Length > 0)
                    {
                        policy.WithOrigins(corsOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                    else
                    {
                        // Development fallback
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                });
            });

            var app = builder.Build();

            // Global exception handler — catches unhandled exceptions, logs them,
            // and returns a safe JSON error response (no stack trace leakage)
            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    logger.Warning("Commander API: Unhandled exception: {Type}: {Message}\n{Stack}",
                        ex.GetType().Name, ex.Message, ex.StackTrace);

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                }
            });

            // Middleware — order matters
            app.UseMiddleware<AdminRateLimitMiddleware>();
            app.UseMiddleware<AuditLogMiddleware>();

            // CORS
            app.UseCors();

            // Auth
            app.UseAuthentication();
            app.UseAuthorization();

            // Map all endpoint groups
            // Auth endpoints: login is anonymous, verify/logout require auth
            app.MapAuthEndpoints();

            // All other endpoint groups require authorization
            app.MapServerEndpoints();
            app.MapPlayerEndpoints();
            app.MapAccountEndpoints();
            app.MapWorldEndpoints();

            _app = app;

            logger.Information("Commander API starting on port {Port}", CommanderApiConfiguration.Port);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Commander API failed to start");
        }
    }
}
