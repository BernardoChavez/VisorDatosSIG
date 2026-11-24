#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Web.Controllers;

[Authorize]
public class AguaController : Controller
{
    private readonly IAguaService _aguaService;

    public AguaController(IAguaService aguaService)
    {
        _aguaService = aguaService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var dashboard = await _aguaService.ObtenerDashboardAsync();
        return View(dashboard);
    }

    public async Task<IActionResult> Asignaciones()
    {
        var trabajadores = await _aguaService.ObtenerTrabajadoresAsync();
        var asignaciones = await _aguaService.ObtenerAsignacionesAsync();
        ViewBag.Trabajadores = trabajadores;
        return View(asignaciones);
    }

    public async Task<IActionResult> Monitoreo()
    {
        var operarios = await _aguaService.ObtenerMonitoreoOperariosAsync();
        return View(operarios);
    }
}
