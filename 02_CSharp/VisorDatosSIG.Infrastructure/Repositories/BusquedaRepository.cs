#nullable enable
using System.Data;
using System.Data.SqlClient;
using VisorDatosSIG.Application.DTOs.Busqueda;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Infrastructure.Data;

namespace VisorDatosSIG.Infrastructure.Repositories;

public class BusquedaRepository : IBusquedaRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public BusquedaRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<(int Total, List<object> Resultados)> BuscarAsync(string termino, string? capa, int pagina, int registrosPorPagina)
    {
        var lista = new List<object>();
        int total = 0;

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_BusquedaGeneral", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Termino", termino);
        object capaParam = (string.IsNullOrWhiteSpace(capa) || capa.Equals("todas", StringComparison.OrdinalIgnoreCase))
            ? DBNull.Value 
            : (object)capa;
        cmd.Parameters.AddWithValue("@Capa", capaParam);
        cmd.Parameters.AddWithValue("@Pagina", pagina);
        cmd.Parameters.AddWithValue("@RegistrosPorPagina", registrosPorPagina);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (total == 0)
            {
                total = reader.GetInt32(reader.GetOrdinal("TotalRegistros"));
            }

            lista.Add(new BusquedaItemDto
            {
                Capa = reader.GetString(reader.GetOrdinal("Capa")),
                IdEntidad = reader.GetInt32(reader.GetOrdinal("IdEntidad")),
                Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
                Descripcion = reader.GetString(reader.GetOrdinal("Descripcion")),
                Detalle = reader.GetString(reader.GetOrdinal("Detalle")),
                Wkt = reader.GetString(reader.GetOrdinal("WKT")),
                LonCentro = reader.IsDBNull(reader.GetOrdinal("LonCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LonCentro")),
                LatCentro = reader.IsDBNull(reader.GetOrdinal("LatCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LatCentro"))
            });
        }

        return (total, lista);
    }

    public async Task<List<object>> IdentificarPorPuntoAsync(double lon, double lat, double toleranciaMetros = 5.0)
    {
        var lista = new List<object>();

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_IdentificarEntidadesPorPunto", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Lon", lon);
        cmd.Parameters.AddWithValue("@Lat", lat);
        cmd.Parameters.AddWithValue("@ToleranciaMetros", toleranciaMetros);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new IdentificacionItemDto
            {
                Capa = reader.IsDBNull(reader.GetOrdinal("Capa")) ? "" : reader.GetString(reader.GetOrdinal("Capa")),
                IdEntidad = reader.GetInt32(reader.GetOrdinal("IdEntidad")),
                Codigo = reader.IsDBNull(reader.GetOrdinal("Codigo")) ? "" : reader.GetString(reader.GetOrdinal("Codigo")),
                Titulo = reader.IsDBNull(reader.GetOrdinal("Titulo")) ? "" : reader.GetString(reader.GetOrdinal("Titulo")),
                Subtitulo = reader.IsDBNull(reader.GetOrdinal("Subtitulo")) ? null : reader.GetString(reader.GetOrdinal("Subtitulo")),
                Resumen = reader.IsDBNull(reader.GetOrdinal("Resumen")) ? null : reader.GetString(reader.GetOrdinal("Resumen")),
                Wkt = reader.IsDBNull(reader.GetOrdinal("WKT")) ? "" : reader.GetString(reader.GetOrdinal("WKT"))
            });
        }

        return lista;
    }
}
