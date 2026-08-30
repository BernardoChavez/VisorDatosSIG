#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Busqueda;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Web.Controllers;

[ApiController]
[Route("api/busqueda")]
[Authorize]
public class BusquedaApiController : ControllerBase
{
    private readonly IBusquedaService _busquedaService;

    public BusquedaApiController(IBusquedaService busquedaService)
    {
        _busquedaService = busquedaService;
    }

    [HttpGet]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? q,
        [FromQuery] string? capa = "todas",
        [FromQuery] int pagina = 1,
        [FromQuery] int registrosPorPagina = 30)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { mensaje = "El término de búsqueda no puede estar vacío." });
        }

        var request = new BusquedaRequestDto
        {
            Termino = q.Trim(),
            Capa = string.IsNullOrWhiteSpace(capa) ? "todas" : capa,
            Pagina = pagina < 1 ? 1 : pagina,
            RegistrosPorPagina = registrosPorPagina < 1 ? 30 : registrosPorPagina
        };

        var resultado = await _busquedaService.BuscarAsync(request);
        return Ok(resultado);
    }

    [HttpGet("identificar")]
    public async Task<IActionResult> IdentificarPorPunto(
        [FromQuery] double lon,
        [FromQuery] double lat,
        [FromQuery] double tolerancia = 5.0)
    {
        var request = new IdentificacionPuntoRequestDto
        {
            Longitud = lon,
            Latitud = lat,
            ToleranciaMetros = tolerancia <= 0 ? 5.0 : tolerancia
        };

        var entidades = await _busquedaService.IdentificarPorPuntoAsync(request);
        return Ok(entidades);
    }
}
