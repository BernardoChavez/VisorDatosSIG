namespace VisorDatosSIG.Domain.Enums;

/// <summary>
/// Capas espaciales soportadas por el sistema VisorDatosSIG.
/// </summary>
public enum TipoCapa
{
    Manzanas = 1,
    Lotes = 2,
    CodigosFijos = 3,
    Vias = 4
}

/// <summary>
/// Modalidad de importación de shapefiles.
/// </summary>
public enum ModalidadMigracion
{
    Reemplazar = 1,
    Anexar = 2
}

/// <summary>
/// Estado del proceso de migración de datos espaciales.
/// </summary>
public enum EstadoMigracion
{
    EnProceso = 1,
    Exitoso = 2,
    Fallido = 3,
    Revertido = 4,
    Cancelado = 5
}

/// <summary>
/// Roles de usuario en el sistema.
/// </summary>
public enum RolUsuario
{
    Administrador = 1,
    Consultor = 2
}

/// <summary>
/// Tipos de eventos para la bitácora de auditoría y seguridad.
/// </summary>
public enum TipoEventoBitacora
{
    LoginExitoso,
    LoginFallido,
    Logout,
    Migracion,
    ModificacionUsuario,
    ConsultaEspacial,
    Exportacion,
    ErrorSistema
}
