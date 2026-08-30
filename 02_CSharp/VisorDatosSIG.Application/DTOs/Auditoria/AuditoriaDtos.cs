namespace VisorDatosSIG.Application.DTOs.Auditoria;

public class BitacoraFiltroDto
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? TipoEvento { get; set; }
    public string? Usuario { get; set; }
    public int Pagina { get; set; } = 1;
    public int RegistrosPorPagina { get; set; } = 50;
}

public class BitacoraItemDto
{
    public long IdBitacora { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Usuario { get; set; }
    public string? IpCliente { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? DetalleTecnico { get; set; }
    public bool Exitoso { get; set; }
}

public class BitacoraPaginadaDto
{
    public int TotalRegistros { get; set; }
    public int PaginaActual { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina);
    public int RegistrosPorPagina { get; set; }
    public List<BitacoraItemDto> Items { get; set; } = new();
}
