#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Agua;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Application.Services;

public class AguaService : IAguaService
{
    private readonly IAguaRepository _aguaRepository;
    private readonly IDijkstraRoutingService _dijkstraService;

    public AguaService(IAguaRepository aguaRepository, IDijkstraRoutingService dijkstraService)
    {
        _aguaRepository = aguaRepository;
        _dijkstraService = dijkstraService;
    }

    public async Task<DashboardAguaDto> ObtenerDashboardAsync()
    {
        return await _aguaRepository.ObtenerDashboardAguaAsync();
    }

    public async Task<List<ConexionAguaDto>> ObtenerConexionesAsync(string? estado = null, BoundingBox? bbox = null, string? busqueda = null)
    {
        return await _aguaRepository.ObtenerConexionesAguaAsync(estado, bbox, busqueda);
    }

    public async Task<ConexionAguaDto?> ObtenerConexionPorIdAsync(int idConexion)
    {
        return await _aguaRepository.ObtenerConexionPorIdAsync(idConexion);
    }

    public async Task<List<TrabajadorDto>> ObtenerTrabajadoresAsync(string? tipo = null)
    {
        return await _aguaRepository.ObtenerTrabajadoresAsync(tipo, soloActivos: true);
    }

    public async Task<List<ConexionAguaDto>> ObtenerCandidatosAsignacionAsync(string tipoServicio, int cantidad, double puntoPartidaLon, double puntoPartidaLat)
    {
        return await _aguaRepository.ObtenerCandidatosAsignacionAsync(tipoServicio, cantidad, puntoPartidaLon, puntoPartidaLat);
    }

    public async Task<RutaCalculadaDijkstraDto> PrevisualizarRutaOptimaAsync(double partidaLon, double partidaLat, List<int> idsConexiones)
    {
        if (idsConexiones == null || idsConexiones.Count == 0)
        {
            return new RutaCalculadaDijkstraDto();
        }

        var todas = await _aguaRepository.ObtenerConexionesAguaAsync();
        var seleccionadas = todas.Where(c => idsConexiones.Contains(c.IdConexion)).ToList();

        return await _dijkstraService.CalcularRutaOptimaAsync(partidaLon, partidaLat, seleccionadas);
    }

    public async Task<AsignacionDto> CrearAsignacionConRutaAsync(CrearAsignacionRequestDto request)
    {
        if (request.IdConexiones == null || request.IdConexiones.Count == 0)
            throw new ArgumentException("Debe seleccionar al menos una conexión de agua para generar la asignación.");

        var trabajador = await _aguaRepository.ObtenerTrabajadorPorIdAsync(request.IdTrabajador);
        if (trabajador == null)
            throw new InvalidOperationException($"No se encontró al trabajador con ID {request.IdTrabajador}");

        var todas = await _aguaRepository.ObtenerConexionesAguaAsync();
        var seleccionadas = todas.Where(c => request.IdConexiones.Contains(c.IdConexion)).ToList();

        // 1. Calcular ruta óptima mediante Dijkstra en red vial
        var ruta = await _dijkstraService.CalcularRutaOptimaAsync(request.PuntoPartidaLon, request.PuntoPartidaLat, seleccionadas);

        // 2. Construir cabecera de la asignación
        var ahora = DateTime.UtcNow;
        var codigo = $"ASIG-{ahora:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        var asignacion = new Asignacion
        {
            CodigoAsignacion = codigo,
            IdTrabajador = request.IdTrabajador,
            TipoServicio = request.TipoServicio,
            FechaAsignacion = DateTime.UtcNow.Date,
            Estado = "Pendiente",
            CantidadPuntos = ruta.PuntosOrdenados.Count,
            PuntoPartidaNombre = string.IsNullOrWhiteSpace(request.PuntoPartidaNombre) ? "Almacén Central / Oficina" : request.PuntoPartidaNombre,
            PuntoPartidaLon = request.PuntoPartidaLon,
            PuntoPartidaLat = request.PuntoPartidaLat,
            DistanciaTotalMetros = ruta.DistanciaTotalMetros,
            RutaOptimaWkt = ruta.RutaLineStringWkt,
            Notas = request.Notas,
            UsuarioCreador = request.Usuario ?? "admin",
            FechaCreacion = ahora
        };

        // 3. Construir detalles con secuencia 1, 2, 3...
        var detalles = new List<AsignacionDetalle>();
        foreach (var p in ruta.PuntosOrdenados)
        {
            var con = seleccionadas.FirstOrDefault(c => c.IdConexion == p.IdConexion);
            detalles.Add(new AsignacionDetalle
            {
                IdConexion = p.IdConexion,
                OrdenSecuencia = p.OrdenSecuencia,
                DistanciaDesdeAnteriorM = p.DistanciaTramoMetros,
                Estado = "Pendiente",
                LecturaAnterior = con?.UltimaLectura
            });
        }

        // 4. Guardar en Base de Datos
        int idAsignacion = await _aguaRepository.CrearAsignacionAsync(asignacion, detalles);

        var creada = await _aguaRepository.ObtenerAsignacionPorIdAsync(idAsignacion);
        return creada ?? throw new InvalidOperationException("Error al recuperar la asignación creada.");
    }

    public async Task<List<AsignacionDto>> ObtenerAsignacionesAsync(string? estado = null, int? idTrabajador = null)
    {
        return await _aguaRepository.ObtenerAsignacionesAsync(estado, idTrabajador);
    }

    public async Task<AsignacionDto?> ObtenerAsignacionPorIdAsync(int idAsignacion)
    {
        return await _aguaRepository.ObtenerAsignacionPorIdAsync(idAsignacion);
    }

    public async Task<RegistroEjecucionCampoResponseDto> RegistrarEjecucionCampoAsync(RegistroEjecucionCampoRequestDto request)
    {
        return await _aguaRepository.RegistrarEjecucionCampoAsync(request);
    }

    public async Task<bool> ActualizarUbicacionMonitoreoAsync(ActualizarUbicacionGpsRequestDto request)
    {
        return await _aguaRepository.ActualizarUbicacionMonitoreoAsync(request);
    }

    public async Task<List<MonitoreoOperarioDto>> ObtenerMonitoreoOperariosAsync()
    {
        return await _aguaRepository.ObtenerMonitoreoOperariosAsync();
    }
}
