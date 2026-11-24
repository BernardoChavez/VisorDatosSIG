#nullable enable
using System;
using System.Collections.Generic;

namespace VisorDatosSIG.Application.DTOs.Agua;

public class DashboardAguaDto
{
    public int TotalConexiones { get; set; }
    public int ConexionesNormales { get; set; }
    public int ConexionesParaCorte { get; set; }
    public int ConexionesCortadas { get; set; }
    public int ConexionesBajaParcial { get; set; }
    public decimal DeudaTotalCorte { get; set; }
    public int AsignacionesActivas { get; set; }
    public int TrabajadoresEnRuta { get; set; }
    public int TotalTrabajadores { get; set; }
}

public class ConexionAguaDto
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
    public string EstadoConexion { get; set; } = "Normal";
    public int MesesMora { get; set; }
    public decimal SaldoPendiente { get; set; }
    public decimal? UltimaLectura { get; set; }
    public DateTime? FechaUltimaLectura { get; set; }
    public double Longitud { get; set; }
    public double Latitud { get; set; }
    public string? Wkt { get; set; }
    public double? DistanciaAproximadaMetros { get; set; }
}

public class TrabajadorDto
{
    public int IdTrabajador { get; set; }
    public string CodigoTrabajador { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string TipoTrabajador { get; set; } = "Cortador";
    public string Estado { get; set; } = "Disponible";
    public string? Vehiculo { get; set; }
    public int? IdUsuario { get; set; }
    public bool Activo { get; set; } = true;
}

public class AsignacionPuntoDto
{
    public int IdDetalle { get; set; }
    public int IdAsignacion { get; set; }
    public int IdConexion { get; set; }
    public string CodigoConexion { get; set; } = string.Empty;
    public string NumeroMedidor { get; set; } = string.Empty;
    public string? CodigoLote { get; set; }
    public string? CodigoManzana { get; set; }
    public string Titular { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int OrdenSecuencia { get; set; }
    public decimal DistanciaDesdeAnteriorM { get; set; }
    public string Estado { get; set; } = "Pendiente";
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
    public double Longitud { get; set; }
    public double Latitud { get; set; }
}

public class AsignacionDto
{
    public int IdAsignacion { get; set; }
    public string CodigoAsignacion { get; set; } = string.Empty;
    public int IdTrabajador { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public string TipoTrabajador { get; set; } = "Cortador";
    public DateTime FechaAsignacion { get; set; }
    public string TipoServicio { get; set; } = "Corte";
    public string Estado { get; set; } = "Pendiente";
    public int CantidadPuntos { get; set; }
    public int PuntosCompletados { get; set; }
    public decimal DistanciaTotalMetros { get; set; }
    public string? RutaOptimaWkt { get; set; }
    public string PuntoPartidaNombre { get; set; } = "Almacén Central / Oficina";
    public double PuntoPartidaLon { get; set; }
    public double PuntoPartidaLat { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public string? Notas { get; set; }
    public List<AsignacionPuntoDto> Puntos { get; set; } = new();
}

public class CrearAsignacionRequestDto
{
    public int IdTrabajador { get; set; }
    public string TipoServicio { get; set; } = "Corte"; // 'Corte' o 'Reconexion'
    public string PuntoPartidaNombre { get; set; } = "Almacén Central / Oficina";
    public double PuntoPartidaLon { get; set; } = -77.03450;
    public double PuntoPartidaLat { get; set; } = -12.09350;
    public List<int> IdConexiones { get; set; } = new();
    public string? Notas { get; set; }
    public string? Usuario { get; set; }
}

public class PuntoRutaDijkstraDto
{
    public int OrdenSecuencia { get; set; }
    public int IdConexion { get; set; }
    public string CodigoConexion { get; set; } = string.Empty;
    public double Longitud { get; set; }
    public double Latitud { get; set; }
    public decimal DistanciaTramoMetros { get; set; }
}

public class RutaCalculadaDijkstraDto
{
    public decimal DistanciaTotalMetros { get; set; }
    public string RutaLineStringWkt { get; set; } = string.Empty;
    public List<PuntoRutaDijkstraDto> PuntosOrdenados { get; set; } = new();
}

public class RegistroEjecucionCampoRequestDto
{
    public int IdDetalle { get; set; }
    public string EstadoResultado { get; set; } = "Ejecutado"; // 'Ejecutado', 'CasaCerrada', 'Imposible', 'Rechazado'
    public decimal? LecturaActual { get; set; }
    public string? NumeroMedidorReportado { get; set; }
    public string? FotoMedidorUrl { get; set; }
    public string? Observacion { get; set; }
    public double GpsLat { get; set; }
    public double GpsLon { get; set; }
    public double GpsPrecisionM { get; set; } = 5.0;
    public double ToleranciaMetrosMax { get; set; } = 60.0;
}

public class RegistroEjecucionCampoResponseDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public decimal DistanciaMetros { get; set; }
    public bool ValidacionGpsExitosa { get; set; }
    public string EstadoFinal { get; set; } = string.Empty;
}

public class MonitoreoOperarioDto
{
    public int IdMonitoreo { get; set; }
    public int IdTrabajador { get; set; }
    public string CodigoTrabajador { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string TipoTrabajador { get; set; } = string.Empty;
    public string? Vehiculo { get; set; }
    public string EstadoOperario { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double VelocidadKmH { get; set; }
    public double? Rumbo { get; set; }
    public double PrecisionM { get; set; }
    public int BateriaPorcentaje { get; set; }
    public DateTime UltimaActualizacion { get; set; }
    public bool EnLinea { get; set; }
    public string? CodigoAsignacion { get; set; }
    public string? TipoServicio { get; set; }
    public int? CantidadPuntos { get; set; }
    public int? PuntosCompletados { get; set; }
    public int? SegundosDesdeUltimaReporte { get; set; }
}

public class ActualizarUbicacionGpsRequestDto
{
    public int IdTrabajador { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double VelocidadKmH { get; set; } = 0.0;
    public double? Rumbo { get; set; }
    public double PrecisionM { get; set; } = 5.0;
    public int BateriaPorcentaje { get; set; } = 100;
}
