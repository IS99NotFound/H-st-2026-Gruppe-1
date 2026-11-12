using System.Security.Claims;
using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq.Language;

namespace H_st_2026_Gruppe_1.Tests;



public class ReportsServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetUnansweredNeedsAsync_ReturnsNeedsWithoutAcceptedOrCompletedMatch()
    {
        _db.Seed(context =>
        {
            context.Categories.Add(TestData.NewCategory());

            context.Needs.AddRange(
                TestData.NewNeed(1, title: "Ingen match"),
                TestData.NewNeed(2, title: "Vurderes"),
                TestData.NewNeed(3, title: "Tildelt"),
                TestData.NewNeed(4, title: "Avslått"),
                TestData.NewNeed(5, title: "Fullført"));

            context.MatchRecords.AddRange(
                TestData.NewMatch(2, 20, "proposed"),
                TestData.NewMatch(3, 30, "accepted"),
                TestData.NewMatch(4, 40, "declined"),
                TestData.NewMatch(5, 50, "completed"));
        });

        using var context = _db.CreateContext();
        var service = new ReportsService(context);

        var result = await service.GetUnansweredNeedsAsync(
            fromDate: null,
            toDate: null,
            category: null,
            location: null);

        // Ingen match, vurdering og avslag regnes som ubesvart.
        // accepted og completed regnes som besvart av den nåværende service-logikken.
        Assert.Equal(new[] { 1, 2, 4 }, result.Select(need => need.Id).OrderBy(id => id));
        Assert.Equal("Ingen match", result.Single(need => need.Id == 1).Title);
        Assert.Equal("Transport", result.Single(need => need.Id == 1).Category);
    }

    [Fact]
    // definere test metoden med en meningsfylt navn 
    public async Task RegistrationsForm_Returns_PartialViewWithCorrectViewName()
    {
        // lage en test database 
       using var context = _db.CreateContext();

       // legge til test brukere i databasen
       context.Users.Add(new User
       {
           Id = 1,
           UserName = "testaktør",
           Email = "aktor@eksempel.no",
           PhoneNumber = "12345678",

       });
       
       
       // lagre endringene i test databasen 
       context.SaveChanges();
       
       // definere avhengigheter som trengs for publicactor controlleren for å fungere 
       var lifesycle = new MatchNeedLifecycle(context);
       var controller = new PublicActorController(context, lifesycle);

       // lage en liste med claims for den test brukeren vi oprettet 
       var claims = new List<Claim>
       {
           new Claim(ClaimTypes.NameIdentifier, "1"),
           new Claim(ClaimTypes.Name, "testaktor"),
           new Claim(ClaimTypes.Role, "PublicActor"),
           
       };
       
       // adgangskort for testbrukeren, asp.net forstår ikke  hvem som er logget inn hvis 
       // det ikke ligger i en claimsprincipal i httpcontext
       var claimsIdentity = new ClaimsIdentity(claims, "TestAuth");
       var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

       // fester adgangskortet på controlleren  
       controller.ControllerContext = new ControllerContext
       {
           HttpContext = new DefaultHttpContext { User = claimsPrincipal }
       };
       
       var result = controller.RegistrationForm();
    }


    [Fact]
    public void RegistrationForm_WithNonExistingUser_ReturnsEmptyFields()
    {
        // Arrange legger ikke inn noen bruker her 
        using var context = _db.CreateContext();
        
        var lifesycle = new MatchNeedLifecycle(context);
        var controller = new PublicActorController(context, lifesycle);
        
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "999") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        var result = controller.RegistrationForm();

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<ResourceNeed>(partialViewResult.Model);
        
        Assert.Equal("", model.Email);
        Assert.Equal("aktor@emial.no", model.PhoneNumber);
    }
}
