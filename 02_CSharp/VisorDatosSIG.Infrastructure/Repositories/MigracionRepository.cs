#nullable enable
using System.Data;
using System.Data.SqlClient;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Infrastructure.Data;

namespace VisorDatosSIG.Infrastructure.Repositories;

public class MigracionRepository : IMigracionRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public MigracionRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> IniciarMigracionAsync(string usuario, string capa, string nombreArchivo, string rutaArchivo, string modalidad, int totalRegistros)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_RegistrarMigracion", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        cmd.Parameters.AddWithValue("@Usuario", usuario);
        cmd.Parameters.AddWithValue("@Capa", capa);
        cmd.Parameters.AddWithValue("@NombreArchivo", nombreArchivo);
        cmd.Parameters.AddWithValue("@RutaArchivo", (object?)rutaArchivo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Modalidad", modalidad);
        cmd.Parameters.AddWithValue("@TotalRegistros", totalRegistros);

        var paramId = new SqlParameter("@IdMigracion", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(paramId);

        await cmd.ExecuteNonQueryAsync();
        return (int)paramId.Value;
    }

    public async Task ActualizarEstadoMigracionAsync(int idMigracion, int exitosos, int omitidos, int fallidos, string estado, string? detalle = null)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ActualizarEstadoMigracion", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        cmd.Parameters.AddWithValue("@IdMigracion", idMigracion);
        cmd.Parameters.AddWithValue("@RegistrosExitosos", exitosos);
        cmd.Parameters.AddWithValue("@RegistrosOmitidos", omitidos);
        cmd.Parameters.AddWithValue("@RegistrosFallidos", fallidos);
        cmd.Parameters.AddWithValue("@Estado", estado);
        cmd.Parameters.AddWithValue("@Detalle", (object?)detalle ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Migracion>> ObtenerHistorialMigracionesAsync(int limite = 50)
    {
        var lista = new List<Migracion>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        var sql = $"SELECT TOP (@Limite) IdMigracion, FechaInicio, FechaFin, Usuario, Capa, NombreArchivo, RutaArchivo, Modalidad, TotalRegistros, RegistrosExitosos, RegistrosOmitidos, RegistrosFallidos, Estado, DuracionMs, Detalle FROM dbo.Migraciones ORDER BY FechaInicio DESC";

        using var cmd = new SqlCommand(sql, conn)
        {
            CommandTimeout = 120
        };
        cmd.Parameters.AddWithValue("@Limite", limite);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Migracion
            {
                IdMigracion = reader.GetInt32(0),
                FechaInicio = reader.GetDateTime(1),
                FechaFin = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                Usuario = reader.GetString(3),
                Capa = reader.GetString(4),
                NombreArchivo = reader.GetString(5),
                RutaArchivo = reader.IsDBNull(6) ? null : reader.GetString(6),
                Modalidad = reader.GetString(7),
                TotalRegistros = reader.GetInt32(8),
                RegistrosExitosos = reader.GetInt32(9),
                RegistrosOmitidos = reader.GetInt32(10),
                RegistrosFallidos = reader.GetInt32(11),
                Estado = reader.GetString(12),
                DuracionMs = reader.IsDBNull(13) ? null : reader.GetInt64(13),
                Detalle = reader.IsDBNull(14) ? null : reader.GetString(14)
            });
        }

        return lista;
    }

    public async Task<int> InsertarManzanasPorLoteAsync(List<Manzana> manzanas, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null)
    {
        if (manzanas.Count == 0) return 0;

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        try
        {
            if (reemplazar)
            {
                using var cmdDelete = new SqlCommand("DELETE FROM dbo.Manzanas", conn, tx)
                {
                    CommandTimeout = 300
                };
                await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            int count = 0;
            int processed = 0;
            var sqlInsert = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Manzanas WHERE CodigoManzana = @CodigoManzana)
                BEGIN
                    INSERT INTO dbo.Manzanas (CodigoManzana, Sector, Barrio, AreaM2, PerimetroM, OrigenId, Geometria, IdMigracion)
                    VALUES (@CodigoManzana, @Sector, @Barrio, @AreaM2, @PerimetroM, @OrigenId, geometry::STGeomFromText(@Wkt, 4326).MakeValid(), @IdMigracion);
                    SELECT 1;
                END
                ELSE IF @Reemplazar = 1
                BEGIN
                    UPDATE dbo.Manzanas
                    SET Sector = @Sector, Barrio = @Barrio, AreaM2 = @AreaM2, PerimetroM = @PerimetroM, OrigenId = @OrigenId, Geometria = geometry::STGeomFromText(@Wkt, 4326).MakeValid(), IdMigracion = @IdMigracion
                    WHERE CodigoManzana = @CodigoManzana;
                    SELECT 1;
                END
                ELSE
                BEGIN
                    SELECT 0;
                END";

            foreach (var m in manzanas)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tx.Rollback();
                    return 0;
                }

                using var cmd = new SqlCommand(sqlInsert, conn, tx)
                {
                    CommandTimeout = 300
                };
                cmd.Parameters.AddWithValue("@CodigoManzana", m.CodigoManzana);
                cmd.Parameters.AddWithValue("@Sector", (object?)m.Sector ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Barrio", (object?)m.Barrio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AreaM2", (object?)m.AreaM2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PerimetroM", (object?)m.PerimetroM ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrigenId", (object?)m.OrigenId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Wkt", m.WktGeometria);
                cmd.Parameters.AddWithValue("@IdMigracion", idMigracion);
                cmd.Parameters.AddWithValue("@Reemplazar", reemplazar ? 1 : 0);

                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                if (Convert.ToInt32(res) == 1)
                {
                    count++;
                }

                processed++;
                int stepM = Math.Max(1, manzanas.Count / 100);
                if (processed % stepM == 0 || processed == manzanas.Count)
                {
                    progreso?.Invoke(processed, manzanas.Count);
                }
            }

            tx.Commit();
            return count;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<int> InsertarLotesPorLoteAsync(List<Lote> lotes, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null)
    {
        if (lotes.Count == 0) return 0;

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        try
        {
            if (reemplazar)
            {
                using var cmdDelete = new SqlCommand("DELETE FROM dbo.Lotes", conn, tx)
                {
                    CommandTimeout = 300
                };
                await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            int count = 0;
            int processed = 0;
            var sqlInsert = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Lotes WHERE CodigoLote = @CodigoLote)
                BEGIN
                    INSERT INTO dbo.Lotes (CodigoLote, CodigoManzana, NumeroPredial, UsoSuelo, AreaTerrenoM2, FrenteM, FondoM, Titular, Direccion, OrigenId, Geometria, IdMigracion)
                    VALUES (@CodigoLote, @CodigoManzana, @NumeroPredial, @UsoSuelo, @AreaTerrenoM2, @FrenteM, @FondoM, @Titular, @Direccion, @OrigenId, geometry::STGeomFromText(@Wkt, 4326).MakeValid(), @IdMigracion);
                    SELECT 1;
                END
                ELSE IF @Reemplazar = 1
                BEGIN
                    UPDATE dbo.Lotes
                    SET CodigoManzana = @CodigoManzana, NumeroPredial = @NumeroPredial, UsoSuelo = @UsoSuelo, AreaTerrenoM2 = @AreaTerrenoM2, FrenteM = @FrenteM, FondoM = @FondoM, Titular = @Titular, Direccion = @Direccion, OrigenId = @OrigenId, Geometria = geometry::STGeomFromText(@Wkt, 4326).MakeValid(), IdMigracion = @IdMigracion
                    WHERE CodigoLote = @CodigoLote;
                    SELECT 1;
                END
                ELSE
                BEGIN
                    SELECT 0;
                END";

            foreach (var l in lotes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tx.Rollback();
                    return 0;
                }

                using var cmd = new SqlCommand(sqlInsert, conn, tx)
                {
                    CommandTimeout = 300
                };
                cmd.Parameters.AddWithValue("@CodigoLote", l.CodigoLote);
                cmd.Parameters.AddWithValue("@CodigoManzana", (object?)l.CodigoManzana ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroPredial", (object?)l.NumeroPredial ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UsoSuelo", (object?)l.UsoSuelo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AreaTerrenoM2", (object?)l.AreaTerrenoM2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FrenteM", (object?)l.FrenteM ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FondoM", (object?)l.FondoM ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Titular", (object?)l.Titular ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Direccion", (object?)l.Direccion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrigenId", (object?)l.OrigenId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Wkt", l.WktGeometria);
                cmd.Parameters.AddWithValue("@IdMigracion", idMigracion);
                cmd.Parameters.AddWithValue("@Reemplazar", reemplazar ? 1 : 0);

                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                if (Convert.ToInt32(res) == 1)
                {
                    count++;
                }

                processed++;
                int stepL = Math.Max(1, lotes.Count / 100);
                if (processed % stepL == 0 || processed == lotes.Count)
                {
                    progreso?.Invoke(processed, lotes.Count);
                }
            }

            tx.Commit();
            return count;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<int> InsertarCodigosFijosPorLoteAsync(List<CodigoFijo> codigosFijos, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null)
    {
        if (codigosFijos.Count == 0) return 0;

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        try
        {
            if (reemplazar)
            {
                using var cmdDelete = new SqlCommand("DELETE FROM dbo.CodigosFijos", conn, tx)
                {
                    CommandTimeout = 300
                };
                await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            int count = 0;
            int processed = 0;
            var sqlInsert = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.CodigosFijos WHERE CodigoFijo = @CodigoFijo)
                BEGIN
                    INSERT INTO dbo.CodigosFijos (CodigoFijo, TipoPunto, Descripcion, Referencia, CotaZ, OrigenId, Geometria, IdMigracion)
                    VALUES (@CodigoFijo, @TipoPunto, @Descripcion, @Referencia, @CotaZ, @OrigenId, geometry::STGeomFromText(@Wkt, 4326).MakeValid(), @IdMigracion);
                    SELECT 1;
                END
                ELSE IF @Reemplazar = 1
                BEGIN
                    UPDATE dbo.CodigosFijos
                    SET TipoPunto = @TipoPunto, Descripcion = @Descripcion, Referencia = @Referencia, CotaZ = @CotaZ, OrigenId = @OrigenId, Geometria = geometry::STGeomFromText(@Wkt, 4326).MakeValid(), IdMigracion = @IdMigracion
                    WHERE CodigoFijo = @CodigoFijo;
                    SELECT 1;
                END
                ELSE
                BEGIN
                    SELECT 0;
                END";

            foreach (var c in codigosFijos)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tx.Rollback();
                    return 0;
                }

                using var cmd = new SqlCommand(sqlInsert, conn, tx)
                {
                    CommandTimeout = 300
                };
                cmd.Parameters.AddWithValue("@CodigoFijo", c.CodigoFijoValor);
                cmd.Parameters.AddWithValue("@TipoPunto", (object?)c.TipoPunto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Descripcion", (object?)c.Descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Referencia", (object?)c.Referencia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CotaZ", (object?)c.CotaZ ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrigenId", (object?)c.OrigenId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Wkt", c.WktGeometria);
                cmd.Parameters.AddWithValue("@IdMigracion", idMigracion);
                cmd.Parameters.AddWithValue("@Reemplazar", reemplazar ? 1 : 0);

                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                if (Convert.ToInt32(res) == 1)
                {
                    count++;
                }

                processed++;
                int stepC = Math.Max(1, codigosFijos.Count / 100);
                if (processed % stepC == 0 || processed == codigosFijos.Count)
                {
                    progreso?.Invoke(processed, codigosFijos.Count);
                }
            }

            tx.Commit();
            return count;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<int> InsertarViasPorLoteAsync(List<Via> vias, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null)
    {
        if (vias.Count == 0) return 0;

        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        try
        {
            if (reemplazar)
            {
                using var cmdDelete = new SqlCommand("DELETE FROM dbo.Vias", conn, tx)
                {
                    CommandTimeout = 300
                };
                await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            int count = 0;
            int processed = 0;
            var sqlInsert = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Vias WHERE CodigoVia = @CodigoVia)
                BEGIN
                    INSERT INTO dbo.Vias (CodigoVia, Nombre, TipoVia, Jerarquia, Sentido, AnchoViaM, Pavimento, LongitudM, OrigenId, Geometria, IdMigracion)
                    VALUES (@CodigoVia, @Nombre, @TipoVia, @Jerarquia, @Sentido, @AnchoViaM, @Pavimento, @LongitudM, @OrigenId, geometry::STGeomFromText(@Wkt, 4326).MakeValid(), @IdMigracion);
                    SELECT 1;
                END
                ELSE IF @Reemplazar = 1
                BEGIN
                    UPDATE dbo.Vias
                    SET Nombre = @Nombre, TipoVia = @TipoVia, Jerarquia = @Jerarquia, Sentido = @Sentido, AnchoViaM = @AnchoViaM, Pavimento = @Pavimento, LongitudM = @LongitudM, OrigenId = @OrigenId, Geometria = geometry::STGeomFromText(@Wkt, 4326).MakeValid(), IdMigracion = @IdMigracion
                    WHERE CodigoVia = @CodigoVia;
                    SELECT 1;
                END
                ELSE
                BEGIN
                    SELECT 0;
                END";

            foreach (var v in vias)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tx.Rollback();
                    return 0;
                }

                using var cmd = new SqlCommand(sqlInsert, conn, tx)
                {
                    CommandTimeout = 300
                };
                cmd.Parameters.AddWithValue("@CodigoVia", v.CodigoVia);
                cmd.Parameters.AddWithValue("@Nombre", v.Nombre);
                cmd.Parameters.AddWithValue("@TipoVia", (object?)v.TipoVia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Jerarquia", (object?)v.Jerarquia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Sentido", (object?)v.Sentido ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AnchoViaM", (object?)v.AnchoViaM ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Pavimento", (object?)v.Pavimento ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LongitudM", (object?)v.LongitudM ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrigenId", (object?)v.OrigenId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Wkt", v.WktGeometria);
                cmd.Parameters.AddWithValue("@IdMigracion", idMigracion);
                cmd.Parameters.AddWithValue("@Reemplazar", reemplazar ? 1 : 0);

                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                if (Convert.ToInt32(res) == 1)
                {
                    count++;
                }

                processed++;
                int stepV = Math.Max(1, vias.Count / 100);
                if (processed % stepV == 0 || processed == vias.Count)
                {
                    progreso?.Invoke(processed, vias.Count);
                }
            }

            tx.Commit();
            return count;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
