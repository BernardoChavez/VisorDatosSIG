#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Capas;
using VisorDatosSIG.Application.DTOs.GeoJson;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CapasController : ControllerBase
{
    private readonly ICapaService _capaService;

    public CapasController(ICapaService capaService)
    {
        _capaService = capaService;
    }

    /// <summary>
    /// Retorna el catálogo completo de capas disponibles con sus estilos y metadatos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CapaCatalogoDto>), 200)]
    public async Task<IActionResult> ObtenerCatalogo()
    {
        var catalogo = await _capaService.ObtenerCatalogoCapasAsync();
        return Ok(catalogo);
    }

    /// <summary>
    /// Retorna una colección GeoJSON RFC 7946 para la capa especificada, filtrada opcionalmente por Bounding Box.
    /// </summary>
    /// <param name="nombreCapa">Nombre de la capa (Manzanas, Lotes, CodigosFijos, Vias)</param>
    /// <param name="bbox">Bounding Box en formato 'minLon,minLat,maxLon,maxLat'</param>
    /// <param name="limite">Límite máximo de geometrías a retornar (default 5000)</param>
    [HttpGet("{nombreCapa}/geojson")]
    [ProducesResponseType(typeof(GeoJsonFeatureCollection), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ObtenerGeoJson(
        [FromRoute] string nombreCapa, 
        [FromQuery] string? bbox = null, 
        [FromQuery] int limite = 5000,
        [FromQuery] bool refresh = false)
    {
        if (refresh)
        {
            _capaService.InvalidarCache(nombreCapa);
        }

        BoundingBox? boundingBox = null;
        if (!string.IsNullOrWhiteSpace(bbox))
        {
            try
            {
                boundingBox = BoundingBox.DesdeCadena(bbox);
            }
            catch (Exception ex)
            {
                return BadRequest(new { exitoso = false, mensaje = $"Formato de Bounding Box inválido: {ex.Message}. Se espera: minLon,minLat,maxLon,maxLat" });
            }
        }

        var geojson = await _capaService.ObtenerGeoJsonCapaAsync(nombreCapa, boundingBox, limite);
        return Ok(geojson);
    }

    /// <summary>
    /// Retorna el detalle alfanumérico y espacial completo de una entidad específica.
    /// </summary>
    /// <param name="nombreCapa">Nombre de la capa</param>
    /// <param name="id">Identificador numérico de la entidad</param>
    [HttpGet("{nombreCapa}/{id:int}")]
    [ProducesResponseType(typeof(EntidadDetalleDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ObtenerDetalleEntidad([FromRoute] string nombreCapa, [FromRoute] int id)
    {
        var detalle = await _capaService.ObtenerDetalleEntidadAsync(nombreCapa, id);
        if (detalle == null)
        {
            return NotFound(new { exitoso = false, mensaje = $"No se encontró la entidad con ID {id} en la capa '{nombreCapa}'." });
        }

        return Ok(detalle);
    }
}
