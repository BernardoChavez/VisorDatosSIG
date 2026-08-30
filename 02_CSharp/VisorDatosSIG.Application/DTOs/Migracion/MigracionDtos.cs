using VisorDatosSIG.Domain.Enums;

namespace VisorDatosSIG.Application.DTOs.Migracion;

public class ValidacionShapefileDto
{
    public string RutaShp { get; set; } = string.Empty;
    public string NombreCapaDetectada { get; set; } = string.Empty;
    public TipoCapa? CapaEnum { get; set; }
    public bool TieneShp { get; set; }
    public bool TieneShx { get; set; }
    public bool TieneDbf { get; set; }
    public bool TienePrj { get; set; }
    public bool EsWgs84 { get; set; }
    public string? ContenidoPrj { get; set; }
    public string? TipoGeometria { get; set; }
    public int CantidadRegistros { get; set; }
    public List<string> CamposDbf { get; set; } = new();
    public bool EsValidoParaCarga => TieneShp && TieneShx && TieneDbf && TienePrj && EsWgs84;
    public List<string> Errores { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}

public class VistaPreviaRegistroDto
{
    public int IndiceFila { get; set; }
    public string? OrigenId { get; set; }
    public Dictionary<string, object?> Atributos { get; set; } = new();
    public string? WktResumen { get; set; }
    public bool GeometriaValida { get; set; }
}

public class MapeoCampoDto
{
    public string CampoOrigenDbf { get; set; } = string.Empty;
    public string CampoDestinoSql { get; set; } = string.Empty;
    public bool EsRequerido { get; set; }
    public string TipoDato { get; set; } = "string";
}

public class IniciarMigracionRequestDto
{
    public string RutaCarpetaOShapefile { get; set; } = string.Empty;
    public TipoCapa Capa { get; set; }
    public ModalidadMigracion Modalidad { get; set; } = ModalidadMigracion.Reemplazar;
    public string Usuario { get; set; } = "admin";
    public List<MapeoCampoDto> MapeoCampos { get; set; } = new();
}

public class ProgresoMigracionDto
{
    public int IdMigracion { get; set; }
    public string Capa { get; set; } = string.Empty;
    public int TotalRegistros { get; set; }
    public int Procesados { get; set; }
    public int Exitosos { get; set; }
    public int Omitidos { get; set; }
    public int Fallidos { get; set; }
    public double Porcentaje => TotalRegistros > 0 ? Math.Round((double)Procesados / TotalRegistros * 100.0, 2) : 0;
    public string MensajeEstado { get; set; } = string.Empty;
    public bool HaTerminado { get; set; }
    public bool EsCancelado { get; set; }
}

public class ResumenMigracionDto
{
    public int IdMigracion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Capa { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public int TotalRegistros { get; set; }
    public int RegistrosExitosos { get; set; }
    public int RegistrosOmitidos { get; set; }
    public int RegistrosFallidos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public long? DuracionMs { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}
