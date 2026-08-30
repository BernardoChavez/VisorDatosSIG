namespace VisorDatosSIG.Application.DTOs.Capas;

public class CapaCatalogoDto
{
    public int IdCapa { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoGeometria { get; set; } = string.Empty;
    public string ColorBorde { get; set; } = string.Empty;
    public string ColorFondo { get; set; } = string.Empty;
    public decimal Opacidad { get; set; }
    public int GrosorBorde { get; set; }
    public int OrdenVisualizacion { get; set; }
    public bool Activo { get; set; }
    public string? Icono { get; set; }
    public string? CamposVisibles { get; set; }
}

public class EntidadDetalleDto
{
    public string Capa { get; set; } = string.Empty;
    public int IdEntidad { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public Dictionary<string, object?> Atributos { get; set; } = new();
    public string? Wkt { get; set; }
    public double? LonCentro { get; set; }
    public double? LatCentro { get; set; }
}
