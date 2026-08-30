#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Busqueda;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusquedaController : ControllerBase
{
    private readonly IBusquedaService _busquedaService;

    public BusquedaController(IBusquedaService busquedaService)
    {
        _busquedaService = busquedaService;
    }

    /// <summary>
    /// Realiza búsquedas alfanuméricas paginadas en todas las capas o en una capa específica.
    /// Retorna además el Bounding Box envolvente combinado de todos los resultados encontrados para auto-zoom cartográfico.
    /// </summary>
    /// <param name="q">Término de búsqueda</param>
    /// <param name="capa">Filtro opcional por capa (Manzanas, Lotes, CodigosFijos, Vias)</param>
    /// <param name="pagina">Número de página (default 1)</param>
    /// <param name="registrosPorPagina">Registros por página (default 20)</param>
    [HttpGet]
    [ProducesResponseType(typeof(BusquedaResultadoPaginadoDto), 200)]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? q, 
        [FromQuery] string? capa = null, 
        [FromQuery] int pagina = 1, 
        [FromQuery] int registrosPorPagina = 20)
    {
        var request = new BusquedaRequestDto
        {
            Termino = q ?? string.Empty,
            Capa = capa,
            Pagina = Math.Max(1, pagina),
            RegistrosPorPagina = Math.Clamp(registrosPorPagina, 1, 100)
        };

        var resultado = await _busquedaService.BuscarAsync(request);
        return Ok(resultado);
    }

    /// <summary>
    /// Identifica entidades geográficas en todas las capas activas bajo un punto de clic o con un radio de tolerancia en metros.
    /// </summary>
    /// <param name="lon">Longitud geográfica en WGS84</param>
    /// <param name="lat">Latitud geográfica en WGS84</param>
    /// <param name="tolerancia">Tolerancia en metros (default 5.0)</param>
    [HttpGet("identificar")]
    [ProducesResponseType(typeof(List<IdentificacionItemDto>), 200)]
    public async Task<IActionResult> Identificar(
        [FromQuery] double lon, 
        [FromQuery] double lat, 
        [FromQuery] double tolerancia = 5.0)
    {
        var request = new IdentificacionPuntoRequestDto
        {
            Longitud = lon,
            Latitud = lat,
            ToleranciaMetros = tolerancia
        };

        var resultados = await _busquedaService.IdentificarPorPuntoAsync(request);
        return Ok(resultados);
    }
}
