using System.Security.Cryptography;

namespace H_st_2026_Gruppe_1.Middleware;

// Adds security headers to every response.
// The Content-Security-Policy (CSP) tells the browser where scripts, styles and images may come from.
// Inline scripts only run with the nonce, a random value that is made for every request.
public class SecurityHeadersMiddleware
{
    public const string NonceItemKey = "CspNonce";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    // nosniff stops the browser from guessing file types, X-Frame-Options stops clickjacking,
    // Referrer-Policy limits what is sent to other sites and X-Xss-Protection is for old browsers.
    // OnStarting makes the headers appear on error pages too.
    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceItemKey] = nonce;

        var csp = CreatePolicy(nonce, _environment.IsDevelopment());

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["Content-Security-Policy"] = csp;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Xss-Protection"] = "1; mode=block";
            return Task.CompletedTask;
        });

        await _next(context);
    }

    // Builds the CSP text. Each part says where one kind of content may come from.
    // Unpkg is Leaflet (map), jsdelivr is Bootstrap Icons, OpenStreetMap gives the map tiles
    // Overpass gives the survival locations and Nominatim gives place names for map positions.
    // In development, localhost is allowed for browser refresh.
    public static string CreatePolicy(string nonce, bool isDevelopment)
    {
        var connectSource = "connect-src 'self' https://overpass-api.de https://nominatim.openstreetmap.org https://unpkg.com/leaflet@1.9.4/dist/leaflet.js.map";
        if (isDevelopment)
        {
            connectSource += " http://localhost:* ws://localhost:*";
        }

        return string.Join("; ", new[]
        {
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'none'",
            "form-action 'self'",
            $"script-src 'self' https://unpkg.com/leaflet@1.9.4/dist/leaflet.js 'nonce-{nonce}'",
            "style-src 'self' https://unpkg.com https://cdn.jsdelivr.net",
            "img-src 'self' data: https://unpkg.com https://tile.openstreetmap.org https://*.tile.openstreetmap.org https://cache.kartverket.no https://server.arcgisonline.com",
            "font-src 'self' data: https://cdn.jsdelivr.net",
            connectSource
        });
    }
}
