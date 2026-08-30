#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Auditoria;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly IMigradorService _migradorService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IAuthService _authService;

    public AdminController(
        IMigradorService migradorService,
        IAuditoriaService auditoriaService,
        IAuthService authService)
    {
        _migradorService = migradorService;
        _auditoriaService = auditoriaService;
        _authService = authService;
    }

    public async Task<IActionResult> Migraciones()
    {
        var historial = await _migradorService.ObtenerHistorialMigracionesAsync();
        return View(historial);
    }

    public async Task<IActionResult> Auditoria([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? tipoEvento, [FromQuery] string? usuario, [FromQuery] int pagina = 1)
    {
        var filtro = new BitacoraFiltroDto
        {
            FechaDesde = desde,
            FechaHasta = hasta,
            TipoEvento = tipoEvento,
            Usuario = usuario,
            Pagina = pagina,
            RegistrosPorPagina = 50
        };

        var bitacora = await _auditoriaService.ObtenerBitacoraAsync(filtro);
        ViewBag.Filtro = filtro;
        return View(bitacora);
    }

    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _authService.ObtenerUsuariosAsync();
        ViewBag.CatalogoPermisos = await _authService.ObtenerCatalogoPermisosAgrupadoAsync();
        return View(usuarios);
    }
}
