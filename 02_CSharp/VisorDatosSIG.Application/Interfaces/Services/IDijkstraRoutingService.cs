#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Agua;

namespace VisorDatosSIG.Application.Interfaces.Services;

/// <summary>
/// Servicio de cálculo de rutas óptimas utilizando el algoritmo de Dijkstra
/// estrictamente sobre la red vial (dbo.Vias), sin atravesar manzanas ni predios.
/// </summary>
public interface IDijkstraRoutingService
{
    /// <summary>
    /// Calcula la ruta óptima secuencial (1, 2, 3...) conectando los puntos solicitados
    /// a partir de un punto de partida a través del grafo vial.
    /// </summary>
    Task<RutaCalculadaDijkstraDto> CalcularRutaOptimaAsync(
        double partidaLon, 
        double partidaLat, 
        List<ConexionAguaDto> puntosDestino);
}
