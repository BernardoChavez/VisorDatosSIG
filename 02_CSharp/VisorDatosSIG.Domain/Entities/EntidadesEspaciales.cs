#nullable enable
using System;

namespace VisorDatosSIG.Domain.Entities;

public abstract class BaseEntity
{
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Capa espacial de Manzanas Urbanas (Polígonos).
/// </summary>
public class Manzana : BaseEntity
{
    public int IdManzana { get; set; }
    public string CodigoManzana { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Barrio { get; set; }
    public decimal? AreaM2 { get; set; }
    public decimal? PerimetroM { get; set; }
    public string? OrigenId { get; set; }
    public string WktGeometria { get; set; } = string.Empty;
    public double? LonCentro { get; set; }
    public double? LatCentro { get; set; }
    public int? IdMigracion { get; set; }

    public Migracion? Migracion { get; set; }
}

/// <summary>
/// Capa espacial de Lotes Prediales (Polígonos).
/// </summary>
public class Lote : BaseEntity
{
    public int IdLote { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public string? CodigoManzana { get; set; }
    public string? NumeroPredial { get; set; }
    public string? UsoSuelo { get; set; }
    public decimal? AreaTerrenoM2 { get; set; }
    public decimal? FrenteM { get; set; }
    public decimal? FondoM { get; set; }
    public string? Titular { get; set; }
    public string? Direccion { get; set; }
    public string? OrigenId { get; set; }
    public string WktGeometria { get; set; } = string.Empty;
    public double? LonCentro { get; set; }
    public double? LatCentro { get; set; }
    public int? IdMigracion { get; set; }

    public Migracion? Migracion { get; set; }
}

/// <summary>
/// Capa espacial de Códigos Fijos e Hitos (Puntos).
/// </summary>
public class CodigoFijo : BaseEntity
{
    public int IdCodigoFijo { get; set; }
    public string CodigoFijoValor { get; set; } = string.Empty;
    public string? TipoPunto { get; set; }
    public string? Descripcion { get; set; }
    public string? Referencia { get; set; }
    public decimal? CotaZ { get; set; }
    public string? OrigenId { get; set; }
    public string WktGeometria { get; set; } = string.Empty;
    public double? LonCentro { get; set; }
    public double? LatCentro { get; set; }
    public int? IdMigracion { get; set; }

    public Migracion? Migracion { get; set; }
}

/// <summary>
/// Capa espacial de Vías y Ejes Viales (Líneas).
/// </summary>
public class Via : BaseEntity
{
    public int IdVia { get; set; }
    public string CodigoVia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? TipoVia { get; set; }
    public string? Jerarquia { get; set; }
    public string? Sentido { get; set; }
    public decimal? AnchoViaM { get; set; }
    public string? Pavimento { get; set; }
    public decimal? LongitudM { get; set; }
    public string? OrigenId { get; set; }
    public string WktGeometria { get; set; } = string.Empty;
    public double? LonCentro { get; set; }
    public double? LatCentro { get; set; }
    public int? IdMigracion { get; set; }

    public Migracion? Migracion { get; set; }
}
