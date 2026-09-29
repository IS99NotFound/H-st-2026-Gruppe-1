// Oppretter byggeren som setter opp applikasjonen
var builder = WebApplication.CreateBuilder(args);

// Legger til støtte for controllere og views (MVC)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Oppsett av hvordan forespørsler behandles.
// I produksjon vises en feilside i stedet for detaljert feilinformasjon.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Standardverdien for HSTS er 30 dager. Kan endres for produksjon, se https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Sender http-forespørsler videre til https
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Gjør css, js og bilder tilgjengelige
app.MapStaticAssets();

// Standardrute: Home-controlleren og Index-metoden brukes hvis ingenting annet er oppgitt
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();