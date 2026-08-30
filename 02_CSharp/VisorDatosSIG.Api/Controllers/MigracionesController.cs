#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Migracion;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigracionesController : ControllerBase
{
    private readonly IMigradorService _migradorService;

    public MigracionesController(IMigradorService migradorService)
    {
        _migradorService = migradorService;
    }

    /// <summary>
    /// Retorna el historial de migraciones e ingestas espaciales ejecutadas en el sistema.
    /// </summary>
    [HttpGet("historial")]
    [ProducesResponseType(typeof(List<ResumenMigracionDto>), 200)]
    public async Task<IActionResult> ObtenerHistorial()
    {
        var historial = await _migradorService.ObtenerHistorialMigracionesAsync();
        return Ok(historial);
    }

    /// <summary>
    /// Inspecciona y valida los archivos de un Shapefile (.shp, .shx, .dbf, .prj) en la ruta indicada.
    /// </summary>
    [HttpPost("validar")]
    [ProducesResponseType(typeof(ValidacionShapefileDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ValidarShapefile([FromBody] string rutaShp)
    {
        if (string.IsNullOrWhiteSpace(rutaShp))
            return BadRequest(new { exitoso = false, mensaje = "Debe proporcionar una ruta de archivo válida." });

        var validacion = await _migradorService.ValidarShapefileAsync(rutaShp);
        return Ok(validacion);
    }

    /// <summary>
    /// Obtiene los primeros 20 registros para previsualización y mapeo de campos.
    /// </summary>
    [HttpPost("vista-previa")]
    [ProducesResponseType(typeof(List<VistaPreviaRegistroDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ObtenerVistaPrevia([FromBody] string rutaShp)
    {
        if (string.IsNullOrWhiteSpace(rutaShp))
            return BadRequest(new { exitoso = false, mensaje = "Debe proporcionar una ruta de archivo válida." });

        var vistaPrevia = await _migradorService.ObtenerVistaPreviaAsync(rutaShp, 20);
        return Ok(vistaPrevia);
    }

    /// <summary>
    /// Ejecuta una migración espacial transaccional de una capa.
    /// </summary>
    [HttpPost("ejecutar")]
    [ProducesResponseType(typeof(ResumenMigracionDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> EjecutarMigracion([FromBody] IniciarMigracionRequestDto request)
    {
        var resumen = await _migradorService.EjecutarMigracionAsync(request);
        return Ok(resumen);
    }
}
