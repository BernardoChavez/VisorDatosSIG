#nullable enable
using System;
using VisorDatosSIG.Domain.Enums;

namespace VisorDatosSIG.Domain.Entities;

/// <summary>
/// Configuración y metadatos cartográficos de las capas en el visor.
/// </summary>
public class CatalogoCapa
{
    public int IdCapa { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoGeometria { get; set; } = "Polygon"; // 'Polygon', 'Point', 'LineString'
    public string ColorBorde { get; set; } = "#3388ff";
    public string ColorFondo { get; set; } = "#3388ff";
    public decimal Opacidad { get; set; } = 0.60m;
    public int GrosorBorde { get; set; } = 2;
    public int OrdenVisualizacion { get; set; } = 1;
    public bool Activo { get; set; } = true;
    public string? Icono { get; set; }
    public string? CamposVisibles { get; set; } // JSON con campos y alias
}

/// <summary>
/// Historial y bitácora de ejecuciones de migración de shapefiles.
/// </summary>
public class Migracion
{
    public int IdMigracion { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Capa { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string? RutaArchivo { get; set; }
    public string Modalidad { get; set; } = "Reemplazar"; // Reemplazar / Anexar
    public int TotalRegistros { get; set; }
    public int RegistrosExitosos { get; set; }
    public int RegistrosOmitidos { get; set; }
    public int RegistrosFallidos { get; set; }
    public string Estado { get; set; } = "En Proceso";
    public long? DuracionMs { get; set; }
    public string? Detalle { get; set; }
}

/// <summary>
/// Rol de seguridad de usuario.
/// </summary>
public class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Usuario del sistema con credenciales seguras y control de accesos.
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Rol? Rol { get; set; }
    public List<string> Permisos { get; set; } = new();
}

/// <summary>
/// Permiso o funcionalidad granular configurable en el sistema SIG.
/// </summary>
public class Permiso
{
    public string CodigoPermiso { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; } = 1;
}

/// <summary>
/// Asignación de permiso granular a un usuario específico.
/// </summary>
public class UsuarioPermiso
{
    public int IdUsuario { get; set; }
    public string CodigoPermiso { get; set; } = string.Empty;
    public bool Permitido { get; set; } = true;

    public Permiso? Permiso { get; set; }
}

/// <summary>
/// Bitácora de eventos y auditoría del sistema.
/// </summary>
public class BitacoraEvento
{
    public long IdBitacora { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public string? Usuario { get; set; }
    public string? IpCliente { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? DetalleTecnico { get; set; }
    public bool Exitoso { get; set; } = true;
}
