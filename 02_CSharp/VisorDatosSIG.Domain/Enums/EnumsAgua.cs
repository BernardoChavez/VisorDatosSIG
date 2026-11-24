namespace VisorDatosSIG.Domain.Enums;

/// <summary>
/// Estados posibles de una Conexión de Agua Potable.
/// </summary>
public enum EstadoConexionAgua
{
    Normal = 1,
    ParaCorte = 2,
    Cortado = 3,
    BajaParcial = 4
}

/// <summary>
/// Especialidad o tipo de operario en campo.
/// </summary>
public enum TipoTrabajador
{
    Cortador = 1,
    Reconector = 2
}

/// <summary>
/// Disponibilidad y estado operativo del trabajador.
/// </summary>
public enum EstadoTrabajador
{
    Disponible = 1,
    EnRuta = 2,
    Ocupado = 3,
    Inactivo = 4
}

/// <summary>
/// Tipo de servicio asignado en una hoja de ruta.
/// </summary>
public enum TipoServicioAsignacion
{
    Corte = 1,
    Reconexion = 2
}

/// <summary>
/// Estado general del lote de trabajo o asignación.
/// </summary>
public enum EstadoAsignacion
{
    Pendiente = 1,
    EnProgreso = 2,
    Completada = 3,
    Cancelada = 4
}

/// <summary>
/// Estado de ejecución de cada punto de corte/reconexión en la ruta.
/// </summary>
public enum EstadoDetalleAsignacion
{
    Pendiente = 1,
    Ejecutado = 2,
    CasaCerrada = 3,
    Imposible = 4,
    Rechazado = 5
}
