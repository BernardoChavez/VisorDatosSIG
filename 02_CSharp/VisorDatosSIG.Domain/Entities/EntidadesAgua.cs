#nullable enable
using System;
using System.Collections.Generic;
using VisorDatosSIG.Domain.Enums;

namespace VisorDatosSIG.Domain.Entities;

/// <summary>
/// Representa una Conexión de Agua Potable y su medidor georreferenciado.
/// </summary>
public class ConexionAgua : BaseEntity
{
    public int IdConexion { get; set; }
    public string CodigoConexion { get; set; } = string.Empty;
    public string NumeroMedidor { get; set; } = string.Empty;
    public int? IdLote { get; set; }
    public string? CodigoLote { get; set; }
    public string? CodigoManzana { get; set; }
    public string Titular { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Doméstico";
    public string EstadoConexion { get; set; } = "Normal"; // 'Normal', 'ParaCorte', 'Cortado', 'BajaParcial'
    public int MesesMora { get; set; } = 0;
    public decimal SaldoPendiente { get; set; } = 0.00m;
    public decimal? UltimaLectura { get; set; }
    public DateTime? FechaUltimaLectura { get; set; }
    public double Longitud { get; set; }
    public double Latitud { get; set; }
    public string WktGeometria { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    // Navegación
    public Lote? Lote { get; set; }
}

/// <summary>
/// Operario de campo para cortes o reconexiones de agua potable.
/// </summary>
public class Trabajador : BaseEntity
{
    public int IdTrabajador { get; set; }
    public string CodigoTrabajador { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string TipoTrabajador { get; set; } = "Cortador"; // 'Cortador', 'Reconector'
    public string Estado { get; set; } = "Disponible";      // 'Disponible', 'EnRuta', 'Ocupado', 'Inactivo'
    public string? Vehiculo { get; set; }                   // 'Moto', 'Camioneta', 'A pie'
    public int? IdUsuario { get; set; }
    public bool Activo { get; set; } = true;

    // Navegación
    public Usuario? Usuario { get; set; }
    public MonitoreoTrabajador? Monitoreo { get; set; }
    public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
}

/// <summary>
/// Asignación o lote de trabajo con ruta óptima de Dijkstra en red vial.
/// </summary>
public class Asignacion
{
    public int IdAsignacion { get; set; }
    public string CodigoAsignacion { get; set; } = string.Empty;
    public int IdTrabajador { get; set; }
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public string TipoServicio { get; set; } = "Corte"; // 'Corte', 'Reconexion'
    public string Estado { get; set; } = "Pendiente";    // 'Pendiente', 'EnProgreso', 'Completada', 'Cancelada'
    public int CantidadPuntos { get; set; } = 0;
    public string PuntoPartidaNombre { get; set; } = "Almacén Central / Oficina";
    public double PuntoPartidaLon { get; set; }
    public double PuntoPartidaLat { get; set; }
    public decimal DistanciaTotalMetros { get; set; } = 0.00m;
    public string? RutaOptimaWkt { get; set; }           // LINESTRING WKT sobre red de vías
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public string? Notas { get; set; }
    public string? UsuarioCreador { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public Trabajador? Trabajador { get; set; }
    public ICollection<AsignacionDetalle> Detalles { get; set; } = new List<AsignacionDetalle>();
}

/// <summary>
/// Punto individual dentro de la asignación ordenada por Dijkstra (1, 2, 3...).
/// Incluye validación de presencia GPS móvil contra el medidor.
/// </summary>
public class AsignacionDetalle
{
    public int IdDetalle { get; set; }
    public int IdAsignacion { get; set; }
    public int IdConexion { get; set; }
    public int OrdenSecuencia { get; set; }              // 1, 2, 3...
    public decimal DistanciaDesdeAnteriorM { get; set; } = 0.00m;
    public string Estado { get; set; } = "Pendiente";   // 'Pendiente', 'Ejecutado', 'CasaCerrada', 'Imposible', 'Rechazado'
    public decimal? LecturaAnterior { get; set; }
    public decimal? LecturaActual { get; set; }
    public string? NumeroMedidorReportado { get; set; }
    public string? FotoMedidorUrl { get; set; }
    public string? Observacion { get; set; }
    public DateTime? FechaHoraEjecucion { get; set; }
    public double? GpsLatEjecucion { get; set; }
    public double? GpsLonEjecucion { get; set; }
    public double? GpsPrecisionM { get; set; }
    public decimal? DistanciaValidacionGpsM { get; set; }
    public bool? ValidacionGpsExitosa { get; set; }

    // Navegación
    public Asignacion? Asignacion { get; set; }
    public ConexionAgua? Conexion { get; set; }
}

/// <summary>
/// Telemetría GPS en tiempo real para seguimiento de operarios en campo.
/// </summary>
public class MonitoreoTrabajador
{
    public int IdMonitoreo { get; set; }
    public int IdTrabajador { get; set; }
    public int? IdAsignacionActiva { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double VelocidadKmH { get; set; } = 0.0;
    public double? Rumbo { get; set; }
    public double PrecisionM { get; set; } = 5.0;
    public int BateriaPorcentaje { get; set; } = 100;
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;
    public bool EnLinea { get; set; } = true;
    public string? WktGeometria { get; set; }

    // Navegación
    public Trabajador? Trabajador { get; set; }
    public Asignacion? AsignacionActiva { get; set; }
}
