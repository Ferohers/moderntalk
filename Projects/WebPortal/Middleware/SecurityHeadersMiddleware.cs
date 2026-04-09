namespace Server.WebPortal.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent MIME type sniffing
        context.Response.Headers.XContentTypeOptions = "nosniff";

        // Prevent clickjacking
        context.Response.Headers.XFrameOptions = "DENY";

        // Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Disable unnecessary browser features
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Content Security Policy - allow self and inline styles only
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "script-src 'self'; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "connect-src 'self'";

        // XSS Protection (legacy browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        await _next(context);
    }
}
