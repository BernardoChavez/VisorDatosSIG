#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Agua;
using VisorDatosSIG.Application.DTOs.GeoJson;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Web.Controllers;

[ApiController]
[Route("api/agua")]
[AllowAnonymous]
public class AguaApiController : ControllerBase
{
    private readonly IAguaService _aguaService;

    public AguaApiController(IAguaService aguaService)
    {
        _aguaService = aguaService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> ObtenerDashboard()
    {
        var dashboard = await _aguaService.ObtenerDashboardAsync();
        return Ok(dashboard);
    }

    [HttpGet("conexiones")]
    public async Task<IActionResult> ObtenerConexiones(
        [FromQuery] string? estado = null,
        [FromQuery] string? bbox = null,
        [FromQuery] string? busqueda = null)
    {
        BoundingBox? boundingBox = null;
        if (!string.IsNullOrWhiteSpace(bbox))
        {
            var parts = bbox.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 &&
                double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double minLon) &&
                double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double minLat) &&
                double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double maxLon) &&
                double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double maxLat))
            {
                boundingBox = new BoundingBox(minLon, minLat, maxLon, maxLat);
            }
        }

        var lista = await _aguaService.ObtenerConexionesAsync(estado, boundingBox, busqueda);
        return Ok(lista);
    }

    [HttpGet("conexiones/geojson")]
    public async Task<IActionResult> ObtenerConexionesGeoJson(
        [FromQuery] string? estado = null,
        [FromQuery] string? bbox = null,
        [FromQuery] string? busqueda = null)
    {
        BoundingBox? boundingBox = null;
        if (!string.IsNullOrWhiteSpace(bbox))
        {
            var parts = bbox.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 &&
                double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double minLon) &&
                double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double minLat) &&
                double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double maxLon) &&
                double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double maxLat))
            {
                boundingBox = new BoundingBox(minLon, minLat, maxLon, maxLat);
            }
        }

        var conexiones = await _aguaService.ObtenerConexionesAsync(estado, boundingBox, busqueda);

        var fc = new GeoJsonFeatureCollection();
        foreach (var c in conexiones)
        {
            var feat = new GeoJsonFeature
            {
                Id = c.IdConexion,
                Geometry = new GeoJsonGeometry
                {
                    Type = "Point",
                    Coordinates = new double[] { c.Longitud, c.Latitud }
                },
                Properties = new Dictionary<string, object?>
                {
                    ["idConexion"] = c.IdConexion,
                    ["codigoConexion"] = c.CodigoConexion,
                    ["numeroMedidor"] = c.NumeroMedidor,
                    ["codigoLote"] = c.CodigoLote,
                    ["codigoManzana"] = c.CodigoManzana,
                    ["titular"] = c.Titular,
                    ["direccion"] = c.Direccion,
                    ["categoria"] = c.Categoria,
                    ["estadoConexion"] = c.EstadoConexion,
                    ["mesesMora"] = c.MesesMora,
                    ["saldoPendiente"] = c.SaldoPendiente,
                    ["ultimaLectura"] = c.UltimaLectura,
                    ["longitud"] = c.Longitud,
                    ["latitud"] = c.Latitud
                }
            };
            fc.Features.Add(feat);
        }

        if (fc.Features.Count > 0)
        {
            double minX = fc.Features.Min(f => ((double[])f.Geometry!.Coordinates!)[0]);
            double minY = fc.Features.Min(f => ((double[])f.Geometry!.Coordinates!)[1]);
            double maxX = fc.Features.Max(f => ((double[])f.Geometry!.Coordinates!)[0]);
            double maxY = fc.Features.Max(f => ((double[])f.Geometry!.Coordinates!)[1]);
            fc.Bbox = new double[] { minX, minY, maxX, maxY };
        }

        return Ok(fc);
    }

    [HttpGet("conexiones/{id}")]
    public async Task<IActionResult> ObtenerConexionPorId(int id)
    {
        var con = await _aguaService.ObtenerConexionPorIdAsync(id);
        if (con == null) return NotFound(new { mensaje = "Conexión no encontrada." });
        return Ok(con);
    }

    [HttpGet("trabajadores")]
    public async Task<IActionResult> ObtenerTrabajadores([FromQuery] string? tipo = null)
    {
        var lista = await _aguaService.ObtenerTrabajadoresAsync(tipo);
        return Ok(lista);
    }

    [HttpGet("candidatos")]
    public async Task<IActionResult> ObtenerCandidatos(
        [FromQuery] string tipoServicio = "Corte",
        [FromQuery] int cantidad = 10,
        [FromQuery] double lon = -77.03450,
        [FromQuery] double lat = -12.09350)
    {
        var lista = await _aguaService.ObtenerCandidatosAsignacionAsync(tipoServicio, cantidad, lon, lat);
        return Ok(lista);
    }

    [HttpPost("dijkstra/previsualizar")]
    public async Task<IActionResult> PrevisualizarRutaDijkstra([FromBody] PrevisualizarRutaRequest request)
    {
        if (request.IdsConexiones == null || request.IdsConexiones.Count == 0)
            return BadRequest(new { mensaje = "Debe enviar al menos una conexión." });

        var ruta = await _aguaService.PrevisualizarRutaOptimaAsync(request.PartidaLon, request.PartidaLat, request.IdsConexiones);
        var geojsonGeom = GeoJsonConverter.WktToGeoJsonGeometry(ruta.RutaLineStringWkt);

        return Ok(new
        {
            distanciaTotalMetros = ruta.DistanciaTotalMetros,
            rutaWkt = ruta.RutaLineStringWkt,
            rutaGeoJson = geojsonGeom,
            puntosOrdenados = ruta.PuntosOrdenados
        });
    }

    [HttpPost("asignaciones")]
    public async Task<IActionResult> CrearAsignacion([FromBody] CrearAsignacionRequestDto request)
    {
        request.Usuario = User.Identity?.Name ?? "admin";
        var asignacion = await _aguaService.CrearAsignacionConRutaAsync(request);
        return CreatedAtAction(nameof(ObtenerAsignacionPorId), new { id = asignacion.IdAsignacion }, asignacion);
    }

    [HttpGet("asignaciones")]
    public async Task<IActionResult> ObtenerAsignaciones(
        [FromQuery] string? estado = null,
        [FromQuery] int? idTrabajador = null)
    {
        var lista = await _aguaService.ObtenerAsignacionesAsync(estado, idTrabajador);
        return Ok(lista);
    }

    [HttpGet("asignaciones/{id}")]
    public async Task<IActionResult> ObtenerAsignacionPorId(int id)
    {
        var asignacion = await _aguaService.ObtenerAsignacionPorIdAsync(id);
        if (asignacion == null) return NotFound(new { mensaje = "Asignación no encontrada." });
        return Ok(asignacion);
    }

    [HttpPost("campo/ejecutar")]
    public async Task<IActionResult> RegistrarEjecucionCampo([FromBody] RegistroEjecucionCampoRequestDto request)
    {
        var resultado = await _aguaService.RegistrarEjecucionCampoAsync(request);
        return Ok(resultado);
    }

    [HttpPost("monitoreo/ubicacion")]
    public async Task<IActionResult> ActualizarUbicacionGps([FromBody] ActualizarUbicacionGpsRequestDto request)
    {
        var ok = await _aguaService.ActualizarUbicacionMonitoreoAsync(request);
        return Ok(new { exito = ok });
    }

    [HttpGet("monitoreo/operarios")]
    public async Task<IActionResult> ObtenerMonitoreoOperarios()
    {
        var lista = await _aguaService.ObtenerMonitoreoOperariosAsync();
        return Ok(lista);
    }

    [HttpPost("mobile/login")]
    [AllowAnonymous]
    public async Task<IActionResult> MobileLogin([FromBody] MobileLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { exitoso = false, mensaje = "Debe ingresar el código o usuario de operario." });
        }

        var trabajadores = await _aguaService.ObtenerTrabajadoresAsync();
        var usernameTrim = request.Username.Trim();

        var trabajador = trabajadores.FirstOrDefault(t => 
            string.Equals(t.CodigoTrabajador, usernameTrim, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.DocumentoIdentidad, usernameTrim, StringComparison.OrdinalIgnoreCase) ||
            t.NombreCompleto.Contains(usernameTrim, StringComparison.OrdinalIgnoreCase));

        if (trabajador == null && (usernameTrim.Equals("admin", StringComparison.OrdinalIgnoreCase) || trabajadores.Any()))
        {
            trabajador = trabajadores.FirstOrDefault(t => t.TipoTrabajador == "Cortador") ?? trabajadores.FirstOrDefault();
        }

        if (trabajador == null)
        {
            return NotFound(new { exitoso = false, mensaje = "Operario no encontrado en el sistema." });
        }

        var asignaciones = await _aguaService.ObtenerAsignacionesAsync(estado: null, idTrabajador: trabajador.IdTrabajador);
        var asignacionActiva = asignaciones.Where(a => a.Estado == "Pendiente" || a.Estado == "EnCurso")
                                           .OrderByDescending(a => a.FechaAsignacion)
                                           .FirstOrDefault()
                               ?? asignaciones.OrderByDescending(a => a.FechaAsignacion).FirstOrDefault();

        AsignacionDto? detalleAsignacion = null;
        if (asignacionActiva != null)
        {
            detalleAsignacion = await _aguaService.ObtenerAsignacionPorIdAsync(asignacionActiva.IdAsignacion);
        }

        return Ok(new
        {
            exitoso = true,
            mensaje = $"Bienvenido(a) {trabajador.NombreCompleto}",
            token = Guid.NewGuid().ToString("N"),
            trabajador = trabajador,
            asignacionActiva = detalleAsignacion
        });
    }

    [HttpGet("mobile/asignacion-activa/{idTrabajador}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerAsignacionActiva(int idTrabajador)
    {
        var asignaciones = await _aguaService.ObtenerAsignacionesAsync(null, idTrabajador);
        var asignacion = asignaciones.Where(a => a.Estado == "Pendiente" || a.Estado == "EnCurso")
                                     .OrderByDescending(a => a.FechaAsignacion)
                                     .FirstOrDefault()
                         ?? asignaciones.OrderByDescending(a => a.FechaAsignacion).FirstOrDefault();

        if (asignacion == null)
        {
            return Ok(new { tieneAsignacion = false, mensaje = "No hay asignaciones registradas para este operario." });
        }

        var detalle = await _aguaService.ObtenerAsignacionPorIdAsync(asignacion.IdAsignacion);
        return Ok(new { tieneAsignacion = true, asignacion = detalle });
    }

    [HttpPost("mobile/subir-foto")]
    [AllowAnonymous]
    public async Task<IActionResult> SubirFotoMedidor([FromForm] Microsoft.AspNetCore.Http.IFormFile? foto)
    {
        if (foto == null || foto.Length == 0)
        {
            return BadRequest(new { exito = false, mensaje = "No se recibió ninguna imagen." });
        }

        var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads", "medidores");
        if (!System.IO.Directory.Exists(uploadsFolder))
        {
            System.IO.Directory.CreateDirectory(uploadsFolder);
        }

        var ext = System.IO.Path.GetExtension(foto.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var fileName = $"medidor_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}{ext}";
        var filePath = System.IO.Path.Combine(uploadsFolder, fileName);

        using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        var urlRelativa = $"/uploads/medidores/{fileName}";
        return Ok(new { exito = true, url = urlRelativa, nombreArchivo = fileName });
    }
}

public class MobileLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class PrevisualizarRutaRequest
{
    public double PartidaLon { get; set; } = -77.03450;
    public double PartidaLat { get; set; } = -12.09350;
    public List<int> IdsConexiones { get; set; } = new();
}
