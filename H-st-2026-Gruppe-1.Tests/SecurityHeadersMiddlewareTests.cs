using H_st_2026_Gruppe_1.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for SecurityHeadersMiddleware: the security headers and the Content-Security-Policy (CSP).
// These are part of the "sikkerhetstesting" in the project.
public class SecurityHeadersMiddlewareTests
{
    // Runs one fake request through the middleware and returns the context.
    // "OnStarting" is fired by hand, because that is when the headers are added.
    private static async Task<DefaultHttpContext> RunRequest(string environmentName = "Production")
    {
        var feature = new RecordingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(feature);

        var middleware = new SecurityHeadersMiddleware(
            _ => Task.CompletedTask,
            new FakeWebHostEnvironment(environmentName));

        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        return context;
    }

    // ---------- CreatePolicy (the CSP text) ----------

    [Theory]
    [InlineData("object-src 'none'")]
    [InlineData("frame-ancestors 'none'")]
    public void CreatePolicy_HasTheStrictRules(string expectedRule)
    {
        var policy = SecurityHeadersMiddleware.CreatePolicy("abc", false);

        Assert.Contains(expectedRule, policy);
    }

    [Theory]
    [InlineData(false)]
    public void CreatePolicy_NeverAllowsUnsafeInlineOrEval(bool isDevelopment)
    {
        var policy = SecurityHeadersMiddleware.CreatePolicy("abc", isDevelopment);

        Assert.DoesNotContain("unsafe-inline", policy);
        Assert.DoesNotContain("unsafe-eval", policy);
    }

    [Fact]
    public void CreatePolicy_InProduction_DoesNotAllowLocalhost()
    {
        var policy = SecurityHeadersMiddleware.CreatePolicy("abc", false);

        Assert.DoesNotContain("localhost", policy);
    }

    // ---------- The middleware itself ----------

    [Fact]
    public async Task Invoke_CallsTheNextStepInThePipeline()
    {
        var nextWasCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            new FakeWebHostEnvironment("Production"));

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.True(nextWasCalled);
    }

    [Fact]
    public async Task Invoke_EveryRequestGetsANewNonce()
    {
        var nonces = new HashSet<string>();

        for (var i = 0; i < 20; i++)
        {
            var context = await RunRequest();
            nonces.Add((string)context.Items[SecurityHeadersMiddleware.NonceItemKey]!);
        }

        Assert.Equal(20, nonces.Count);
    }

    [Fact]
    public async Task Invoke_AddsTheContentSecurityPolicyWithTheSameNonce()
    {
        var context = await RunRequest();
        var nonce = (string)context.Items[SecurityHeadersMiddleware.NonceItemKey]!;

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();

        Assert.Contains("'nonce-" + nonce + "'", csp);
        Assert.Contains("default-src 'self'", csp);
    }

    [Fact]
    public async Task Invoke_AddsNosniffHeader()
    {
        var context = await RunRequest();

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
    }
}
