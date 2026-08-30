#nullable enable
using System.Data;
using System.Data.SqlClient;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Infrastructure.Data;

namespace VisorDatosSIG.Infrastructure.Repositories;

public class BitacoraRepository : IBitacoraRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public BitacoraRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task RegistrarEventoAsync(BitacoraEvento evento)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_RegistrarBitacoraEvento", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Usuario", (object?)evento.Usuario ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IpCliente", (object?)evento.IpCliente ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TipoEvento", evento.TipoEvento);
        cmd.Parameters.AddWithValue("@Modulo", evento.Modulo);
        cmd.Parameters.AddWithValue("@Descripcion", evento.Descripcion);
        cmd.Parameters.AddWithValue("@DetalleTecnico", (object?)evento.DetalleTecnico ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Exitoso", evento.Exitoso ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(int Total, List<BitacoraEvento> Items)> ObtenerBitacoraAsync(
        DateTime? desde, 
        DateTime? hasta, 
        string? tipoEvento, 
        string? usuario, 
        int pagina, 
        int registrosPorPagina)
    {
        var lista = new List<BitacoraEvento>();
        int total = 0;

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerBitacora", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@FechaDesde", (object?)desde ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FechaHasta", (object?)hasta ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TipoEvento", string.IsNullOrWhiteSpace(tipoEvento) ? (object)DBNull.Value : tipoEvento);
        cmd.Parameters.AddWithValue("@Usuario", string.IsNullOrWhiteSpace(usuario) ? (object)DBNull.Value : usuario);
        cmd.Parameters.AddWithValue("@Pagina", pagina);
        cmd.Parameters.AddWithValue("@RegistrosPorPagina", registrosPorPagina);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (total == 0)
            {
                total = reader.GetInt32(reader.GetOrdinal("TotalRegistros"));
            }

            lista.Add(new BitacoraEvento
            {
                IdBitacora = reader.GetInt64(reader.GetOrdinal("IdBitacora")),
                FechaHora = reader.GetDateTime(reader.GetOrdinal("FechaHora")),
                Usuario = reader.IsDBNull(reader.GetOrdinal("Usuario")) ? null : reader.GetString(reader.GetOrdinal("Usuario")),
                IpCliente = reader.IsDBNull(reader.GetOrdinal("IpCliente")) ? null : reader.GetString(reader.GetOrdinal("IpCliente")),
                TipoEvento = reader.GetString(reader.GetOrdinal("TipoEvento")),
                Modulo = reader.GetString(reader.GetOrdinal("Modulo")),
                Descripcion = reader.GetString(reader.GetOrdinal("Descripcion")),
                DetalleTecnico = reader.IsDBNull(reader.GetOrdinal("DetalleTecnico")) ? null : reader.GetString(reader.GetOrdinal("DetalleTecnico")),
                Exitoso = reader.GetBoolean(reader.GetOrdinal("Exitoso"))
            });
        }

        return (total, lista);
    }
}
