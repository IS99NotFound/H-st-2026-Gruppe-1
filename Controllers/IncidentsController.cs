using H_st_2026_Gruppe_1.Data;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Controllers;

public class IncidentsController : Controller
{
    private readonly AppDbContext _db;

    public IncidentsController(AppDbContext db)
    {
        _db = db;
    }
    
    public IActionResult Index()
    {
        var incidents = _db.Incidents
            .OrderByDescending(incident => incident.CreatedAt)
            .ToList();        
        
        return View(incidents);
    }
}