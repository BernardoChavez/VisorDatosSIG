#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Agua;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Application.Interfaces.Services;

public interface IAguaService
{
    Task<DashboardAguaDto> ObtenerDashboardAsync();
    Task<List<ConexionAguaDto>> ObtenerConexionesAsync(string? estado = null, BoundingBox? bbox = null, string? busqueda = null);
    Task<ConexionAguaDto?> ObtenerConexionPorIdAsync(int idConexion);
    Task<List<TrabajadorDto>> ObtenerTrabajadoresAsync(string? tipo = null);
    Task<List<ConexionAguaDto>> ObtenerCandidatosAsignacionAsync(string tipoServicio, int cantidad, double puntoPartidaLon, double puntoPartidaLat);
    Task<RutaCalculadaDijkstraDto> PrevisualizarRutaOptimaAsync(double partidaLon, double partidaLat, List<int> idsConexiones);
    Task<AsignacionDto> CrearAsignacionConRutaAsync(CrearAsignacionRequestDto request);
    Task<List<AsignacionDto>> ObtenerAsignacionesAsync(string? estado = null, int? idTrabajador = null);
    Task<AsignacionDto?> ObtenerAsignacionPorIdAsync(int idAsignacion);
    Task<RegistroEjecucionCampoResponseDto> RegistrarEjecucionCampoAsync(RegistroEjecucionCampoRequestDto request);
    Task<bool> ActualizarUbicacionMonitoreoAsync(ActualizarUbicacionGpsRequestDto request);
    Task<List<MonitoreoOperarioDto>> ObtenerMonitoreoOperariosAsync();
}
