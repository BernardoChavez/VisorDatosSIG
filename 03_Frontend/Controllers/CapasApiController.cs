#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Web.Controllers;

[ApiController]
[Route("api/capas")]
[Authorize]
public class CapasApiController : ControllerBase
{
    private readonly ICapaService _capaService;

    public CapasApiController(ICapaService capaService)
    {
        _capaService = capaService;
    }

    private bool TienePermisoCapa(string nombreCapa)
    {
        if (User.IsInRole("Administrador")) return true;
        var codigoPermiso = $"CAPA_{nombreCapa}";
        return User.HasClaim(c => c.Type == "Permiso" && string.Equals(c.Value, codigoPermiso, StringComparison.OrdinalIgnoreCase));
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerCatalogo()
    {
        var catalogo = await _capaService.ObtenerCatalogoCapasAsync();
        
        // Si no es admin, filtramos solo las capas que tiene autorizadas
        if (!User.IsInRole("Administrador"))
        {
            var permisosClaims = User.FindAll("Permiso").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (permisosClaims.Count > 0)
            {
                catalogo = catalogo.Where(c => permisosClaims.Contains($"CAPA_{c.Nombre}")).ToList();
            }
        }

        return Ok(catalogo);
    }

    [HttpGet("{nombreCapa}/geojson")]
    public async Task<IActionResult> ObtenerGeoJson(
        string nombreCapa,
        [FromQuery] string? bbox = null,
        [FromQuery] int limite = 5000,
        [FromQuery] bool refresh = false)
    {
        if (!TienePermisoCapa(nombreCapa))
        {
            return StatusCode(403, new { mensaje = $"No tiene permisos para acceder a la capa cartográfica '{nombreCapa}'." });
        }

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
            catch
            {
                // Si el formato bbox es inválido, continúa sin filtro bbox
            }
        }

        var geojson = await _capaService.ObtenerGeoJsonCapaAsync(nombreCapa, boundingBox, limite);
        return Ok(geojson);
    }

    [HttpGet("{nombreCapa}/{id:int}")]
    public async Task<IActionResult> ObtenerDetalle(string nombreCapa, int id)
    {
        if (!TienePermisoCapa(nombreCapa))
        {
            return StatusCode(403, new { mensaje = $"No tiene permisos para acceder a los detalles de la capa '{nombreCapa}'." });
        }

        var detalle = await _capaService.ObtenerDetalleEntidadAsync(nombreCapa, id);
        if (detalle == null)
        {
            return NotFound(new { mensaje = $"Entidad con ID {id} en capa '{nombreCapa}' no encontrada." });
        }
        return Ok(detalle);
    }
}
