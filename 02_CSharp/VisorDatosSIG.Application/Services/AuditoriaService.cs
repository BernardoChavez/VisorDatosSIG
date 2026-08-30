using VisorDatosSIG.Application.DTOs.Auditoria;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.Entities;

namespace VisorDatosSIG.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IBitacoraRepository _bitacoraRepository;

    public AuditoriaService(IBitacoraRepository bitacoraRepository)
    {
        _bitacoraRepository = bitacoraRepository;
    }

    public async Task RegistrarEventoAsync(
        string tipoEvento, 
        string modulo, 
        string descripcion, 
        string? usuario = null, 
        string? ipCliente = null, 
        string? detalleTecnico = null, 
        bool exitoso = true)
    {
        var evento = new BitacoraEvento
        {
            FechaHora = DateTime.UtcNow,
            TipoEvento = tipoEvento,
            Modulo = modulo,
            Descripcion = descripcion,
            Usuario = usuario,
            IpCliente = ipCliente,
            DetalleTecnico = detalleTecnico,
            Exitoso = exitoso
        };

        await _bitacoraRepository.RegistrarEventoAsync(evento);
    }

    public async Task<BitacoraPaginadaDto> ObtenerBitacoraAsync(BitacoraFiltroDto filtro)
    {
        var (total, items) = await _bitacoraRepository.ObtenerBitacoraAsync(
            filtro.FechaDesde,
            filtro.FechaHasta,
            filtro.TipoEvento,
            filtro.Usuario,
            filtro.Pagina,
            filtro.RegistrosPorPagina);

        return new BitacoraPaginadaDto
        {
            TotalRegistros = total,
            PaginaActual = filtro.Pagina,
            RegistrosPorPagina = filtro.RegistrosPorPagina,
            Items = items.Select(b => new BitacoraItemDto
            {
                IdBitacora = b.IdBitacora,
                FechaHora = b.FechaHora,
                Usuario = b.Usuario,
                IpCliente = b.IpCliente,
                TipoEvento = b.TipoEvento,
                Modulo = b.Modulo,
                Descripcion = b.Descripcion,
                DetalleTecnico = b.DetalleTecnico,
                Exitoso = b.Exitoso
            }).ToList()
        };
    }
}
