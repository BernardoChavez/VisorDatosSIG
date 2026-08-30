using VisorDatosSIG.Application.DTOs.Busqueda;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Application.Services;

public class BusquedaService : IBusquedaService
{
    private readonly IBusquedaRepository _busquedaRepository;

    public BusquedaService(IBusquedaRepository busquedaRepository)
    {
        _busquedaRepository = busquedaRepository;
    }

    public async Task<BusquedaResultadoPaginadoDto> BuscarAsync(BusquedaRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Termino))
        {
            return new BusquedaResultadoPaginadoDto
            {
                TotalRegistros = 0,
                PaginaActual = request.Pagina,
                RegistrosPorPagina = request.RegistrosPorPagina,
                Resultados = new List<BusquedaItemDto>()
            };
        }

        var (total, rawResultados) = await _busquedaRepository.BuscarAsync(
            request.Termino, 
            request.Capa, 
            request.Pagina, 
            request.RegistrosPorPagina);

        var items = new List<BusquedaItemDto>();
        double minLon = double.MaxValue, minLat = double.MaxValue;
        double maxLon = double.MinValue, maxLat = double.MinValue;
        bool tieneCoords = false;

        foreach (var r in rawResultados)
        {
            if (r is BusquedaItemDto item)
            {
                items.Add(item);

                if (item.LonCentro.HasValue && item.LatCentro.HasValue)
                {
                    tieneCoords = true;
                    if (item.LonCentro.Value < minLon) minLon = item.LonCentro.Value;
                    if (item.LonCentro.Value > maxLon) maxLon = item.LonCentro.Value;
                    if (item.LatCentro.Value < minLat) minLat = item.LatCentro.Value;
                    if (item.LatCentro.Value > maxLat) maxLat = item.LatCentro.Value;
                }
            }
        }

        double[]? bbox = null;
        if (tieneCoords)
        {
            // Margen mínimo si es un solo punto
            if (minLon == maxLon) { minLon -= 0.001; maxLon += 0.001; }
            if (minLat == maxLat) { minLat -= 0.001; maxLat += 0.001; }
            bbox = new double[] { minLon, minLat, maxLon, maxLat };
        }

        return new BusquedaResultadoPaginadoDto
        {
            TotalRegistros = total,
            PaginaActual = request.Pagina,
            RegistrosPorPagina = request.RegistrosPorPagina,
            Resultados = items,
            BboxCombinado = bbox
        };
    }

    public async Task<List<IdentificacionItemDto>> IdentificarPorPuntoAsync(IdentificacionPuntoRequestDto request)
    {
        var rawResultados = await _busquedaRepository.IdentificarPorPuntoAsync(
            request.Longitud, 
            request.Latitud, 
            request.ToleranciaMetros);

        var list = new List<IdentificacionItemDto>();
        foreach (var r in rawResultados)
        {
            if (r is IdentificacionItemDto item)
            {
                list.Add(item);
            }
        }

        return list;
    }
}
