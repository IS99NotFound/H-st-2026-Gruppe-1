using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

// App builder 
var builder = WebApplication.CreateBuilder(args);

// Controllers and views (MVC)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Forces application to use period for decimal values (en-us)
// Independent of device language, resolves validation error for coordinates
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = new[] { new CultureInfo("en-US") },
    SupportedUICultures = new[] { new CultureInfo("en-US") }
};
localizationOptions.RequestCultureProviders.Clear();
app.UseRequestLocalization(localizationOptions);

// Setup for requests processing 
// Only show error page while in production 
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Standard value for HSTS; 30 days. Can be changed for production
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Show yellow/red error screencapture with exact line number when crashes occurs 
}

// Forwards http-requests to https
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Makes css, js and pictures available
app.MapStaticAssets();

// Standard route: Home-controller and Index-method is used when nohing else is mentioned 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
