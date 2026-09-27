using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Controllers;

public class IncidentsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}