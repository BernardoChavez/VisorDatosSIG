namespace VisorDatosSIG.Application.DTOs.Busqueda;

public class BusquedaRequestDto
{
    public string Termino { get; set; } = string.Empty;
    public string? Capa { get; set; }
    public int Pagina { get; set; } = 1;
    public int RegistrosPorPagina { get; set; } = 20;
}

public class BusquedaItemDto
{
    public string Capa { get; set; } = string.Empty;
    public int IdEntidad { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string? Wkt { get; set; }
    public double? LonCentro { get; set; }
    public double? LatCentro { get; set; }
}

public class BusquedaResultadoPaginadoDto
{
    public int TotalRegistros { get; set; }
    public int PaginaActual { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina);
    public int RegistrosPorPagina { get; set; }
    public List<BusquedaItemDto> Resultados { get; set; } = new();
    public double[]? BboxCombinado { get; set; }
}

public class IdentificacionPuntoRequestDto
{
    public double Longitud { get; set; }
    public double Latitud { get; set; }
    public double ToleranciaMetros { get; set; } = 5.0;
}

public class IdentificacionItemDto
{
    public string Capa { get; set; } = string.Empty;
    public int IdEntidad { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Resumen { get; set; }
    public string? Wkt { get; set; }
}
