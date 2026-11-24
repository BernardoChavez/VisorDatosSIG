#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Agua;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Application.Interfaces.Repositories;

public interface IAguaRepository
{
    Task<DashboardAguaDto> ObtenerDashboardAguaAsync();
    Task<List<ConexionAguaDto>> ObtenerConexionesAguaAsync(string? estado = null, BoundingBox? bbox = null, string? busqueda = null);
    Task<ConexionAguaDto?> ObtenerConexionPorIdAsync(int idConexion);
    Task<List<TrabajadorDto>> ObtenerTrabajadoresAsync(string? tipo = null, bool soloActivos = true);
    Task<TrabajadorDto?> ObtenerTrabajadorPorIdAsync(int idTrabajador);
    Task<List<ConexionAguaDto>> ObtenerCandidatosAsignacionAsync(string tipoServicio, int cantidad, double puntoPartidaLon, double puntoPartidaLat);
    Task<int> CrearAsignacionAsync(Asignacion asignacion, List<AsignacionDetalle> detalles);
    Task<List<AsignacionDto>> ObtenerAsignacionesAsync(string? estado = null, int? idTrabajador = null);
    Task<AsignacionDto?> ObtenerAsignacionPorIdAsync(int idAsignacion);
    Task<RegistroEjecucionCampoResponseDto> RegistrarEjecucionCampoAsync(RegistroEjecucionCampoRequestDto request);
    Task<bool> ActualizarUbicacionMonitoreoAsync(ActualizarUbicacionGpsRequestDto request);
    Task<List<MonitoreoOperarioDto>> ObtenerMonitoreoOperariosAsync();
    Task<List<Via>> ObtenerRedVialAsync();
}
