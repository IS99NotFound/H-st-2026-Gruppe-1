using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

// Oppretter byggeren som setter opp applikasjonen
var builder = WebApplication.CreateBuilder(args);

// Legger til støtte for controllere og views (MVC)
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

// Tvinger appen til å alltid bruke punktum som desimaltegn (en-US),
// uavhengig av hvilket språk maskinen/serveren har, løser valideringsfeil på koordinater
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = new[] { new CultureInfo("en-US") },
    SupportedUICultures = new[] { new CultureInfo("en-US") }
};
localizationOptions.RequestCultureProviders.Clear();
app.UseRequestLocalization(localizationOptions);

// Oppsett av hvordan forespørsler behandles.
// I produksjon vises en feilside i stedet for detaljert feilinformasjon.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Standardverdien for HSTS er 30 dager. Kan endres for produksjon, se https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Viser gult/rødt feilskjermbilde med eksakt linjenummer ved krasj
}

// Sender http-forespørsler videre til https
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Gjør css, js og bilder tilgjengelige
app.MapStaticAssets();

// Standardrute: Home-controlleren og Index-metoden brukes hvis ingenting annet er oppgitt
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
