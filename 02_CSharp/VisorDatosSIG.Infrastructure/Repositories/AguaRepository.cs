#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Agua;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.ValueObjects;
using VisorDatosSIG.Infrastructure.Data;

namespace VisorDatosSIG.Infrastructure.Repositories;

public class AguaRepository : IAguaRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public AguaRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardAguaDto> ObtenerDashboardAguaAsync()
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerDashboardAgua", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DashboardAguaDto
            {
                TotalConexiones = reader.GetInt32(reader.GetOrdinal("TotalConexiones")),
                ConexionesNormales = reader.GetInt32(reader.GetOrdinal("ConexionesNormales")),
                ConexionesParaCorte = reader.GetInt32(reader.GetOrdinal("ConexionesParaCorte")),
                ConexionesCortadas = reader.GetInt32(reader.GetOrdinal("ConexionesCortadas")),
                ConexionesBajaParcial = reader.GetInt32(reader.GetOrdinal("ConexionesBajaParcial")),
                DeudaTotalCorte = reader.GetDecimal(reader.GetOrdinal("DeudaTotalCorte")),
                AsignacionesActivas = reader.GetInt32(reader.GetOrdinal("AsignacionesActivas")),
                TrabajadoresEnRuta = reader.GetInt32(reader.GetOrdinal("TrabajadoresEnRuta")),
                TotalTrabajadores = reader.GetInt32(reader.GetOrdinal("TotalTrabajadores"))
            };
        }

        return new DashboardAguaDto();
    }

    public async Task<List<ConexionAguaDto>> ObtenerConexionesAguaAsync(string? estado = null, BoundingBox? bbox = null, string? busqueda = null)
    {
        var lista = new List<ConexionAguaDto>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerConexionesAgua", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Estado", (object?)estado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Busqueda", (object?)busqueda ?? DBNull.Value);

        if (bbox != null)
        {
            cmd.Parameters.AddWithValue("@MinLon", bbox.MinLon);
            cmd.Parameters.AddWithValue("@MinLat", bbox.MinLat);
            cmd.Parameters.AddWithValue("@MaxLon", bbox.MaxLon);
            cmd.Parameters.AddWithValue("@MaxLat", bbox.MaxLat);
        }
        else
        {
            cmd.Parameters.AddWithValue("@MinLon", DBNull.Value);
            cmd.Parameters.AddWithValue("@MinLat", DBNull.Value);
            cmd.Parameters.AddWithValue("@MaxLon", DBNull.Value);
            cmd.Parameters.AddWithValue("@MaxLat", DBNull.Value);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ConexionAguaDto
            {
                IdConexion = reader.GetInt32(reader.GetOrdinal("IdConexion")),
                CodigoConexion = reader.GetString(reader.GetOrdinal("CodigoConexion")),
                NumeroMedidor = reader.GetString(reader.GetOrdinal("NumeroMedidor")),
                CodigoLote = reader.IsDBNull(reader.GetOrdinal("CodigoLote")) ? null : reader.GetString(reader.GetOrdinal("CodigoLote")),
                CodigoManzana = reader.IsDBNull(reader.GetOrdinal("CodigoManzana")) ? null : reader.GetString(reader.GetOrdinal("CodigoManzana")),
                Titular = reader.GetString(reader.GetOrdinal("Titular")),
                Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
                EstadoConexion = reader.GetString(reader.GetOrdinal("EstadoConexion")),
                MesesMora = reader.GetInt32(reader.GetOrdinal("MesesMora")),
                SaldoPendiente = reader.GetDecimal(reader.GetOrdinal("SaldoPendiente")),
                UltimaLectura = reader.IsDBNull(reader.GetOrdinal("UltimaLectura")) ? null : reader.GetDecimal(reader.GetOrdinal("UltimaLectura")),
                Longitud = reader.GetDouble(reader.GetOrdinal("Longitud")),
                Latitud = reader.GetDouble(reader.GetOrdinal("Latitud")),
                Wkt = reader.IsDBNull(reader.GetOrdinal("WKT")) ? null : reader.GetString(reader.GetOrdinal("WKT"))
            });
        }

        return lista;
    }

    public async Task<ConexionAguaDto?> ObtenerConexionPorIdAsync(int idConexion)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        const string sql = @"
            SELECT 
                c.IdConexion, c.CodigoConexion, c.NumeroMedidor, c.IdLote, c.CodigoLote, c.CodigoManzana,
                c.Titular, c.Direccion, c.Categoria, c.EstadoConexion, c.MesesMora, c.SaldoPendiente,
                c.UltimaLectura, c.FechaUltimaLectura, c.Longitud, c.Latitud, c.Geometria.STAsText() AS Wkt
            FROM dbo.ConexionesAgua c
            WHERE c.IdConexion = @IdConexion;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdConexion", idConexion);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ConexionAguaDto
            {
                IdConexion = reader.GetInt32(reader.GetOrdinal("IdConexion")),
                CodigoConexion = reader.GetString(reader.GetOrdinal("CodigoConexion")),
                NumeroMedidor = reader.GetString(reader.GetOrdinal("NumeroMedidor")),
                IdLote = reader.IsDBNull(reader.GetOrdinal("IdLote")) ? null : reader.GetInt32(reader.GetOrdinal("IdLote")),
                CodigoLote = reader.IsDBNull(reader.GetOrdinal("CodigoLote")) ? null : reader.GetString(reader.GetOrdinal("CodigoLote")),
                CodigoManzana = reader.IsDBNull(reader.GetOrdinal("CodigoManzana")) ? null : reader.GetString(reader.GetOrdinal("CodigoManzana")),
                Titular = reader.GetString(reader.GetOrdinal("Titular")),
                Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
                EstadoConexion = reader.GetString(reader.GetOrdinal("EstadoConexion")),
                MesesMora = reader.GetInt32(reader.GetOrdinal("MesesMora")),
                SaldoPendiente = reader.GetDecimal(reader.GetOrdinal("SaldoPendiente")),
                UltimaLectura = reader.IsDBNull(reader.GetOrdinal("UltimaLectura")) ? null : reader.GetDecimal(reader.GetOrdinal("UltimaLectura")),
                FechaUltimaLectura = reader.IsDBNull(reader.GetOrdinal("FechaUltimaLectura")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaUltimaLectura")),
                Longitud = reader.GetDouble(reader.GetOrdinal("Longitud")),
                Latitud = reader.GetDouble(reader.GetOrdinal("Latitud")),
                Wkt = reader.IsDBNull(reader.GetOrdinal("Wkt")) ? null : reader.GetString(reader.GetOrdinal("Wkt"))
            };
        }

        return null;
    }

    public async Task<List<TrabajadorDto>> ObtenerTrabajadoresAsync(string? tipo = null, bool soloActivos = true)
    {
        var lista = new List<TrabajadorDto>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();

        string sql = @"
            SELECT IdTrabajador, CodigoTrabajador, NombreCompleto, DocumentoIdentidad, Telefono, TipoTrabajador, Estado, Vehiculo, IdUsuario, Activo
            FROM dbo.Trabajadores
            WHERE (@SoloActivos = 0 OR Activo = 1)
              AND (@Tipo IS NULL OR TipoTrabajador = @Tipo)
            ORDER BY NombreCompleto ASC;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SoloActivos", soloActivos ? 1 : 0);
        cmd.Parameters.AddWithValue("@Tipo", (object?)tipo ?? DBNull.Value);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new TrabajadorDto
            {
                IdTrabajador = reader.GetInt32(reader.GetOrdinal("IdTrabajador")),
                CodigoTrabajador = reader.GetString(reader.GetOrdinal("CodigoTrabajador")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                DocumentoIdentidad = reader.GetString(reader.GetOrdinal("DocumentoIdentidad")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                TipoTrabajador = reader.GetString(reader.GetOrdinal("TipoTrabajador")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                Vehiculo = reader.IsDBNull(reader.GetOrdinal("Vehiculo")) ? null : reader.GetString(reader.GetOrdinal("Vehiculo")),
                IdUsuario = reader.IsDBNull(reader.GetOrdinal("IdUsuario")) ? null : reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
            });
        }

        return lista;
    }

    public async Task<TrabajadorDto?> ObtenerTrabajadorPorIdAsync(int idTrabajador)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        const string sql = @"
            SELECT IdTrabajador, CodigoTrabajador, NombreCompleto, DocumentoIdentidad, Telefono, TipoTrabajador, Estado, Vehiculo, IdUsuario, Activo
            FROM dbo.Trabajadores
            WHERE IdTrabajador = @IdTrabajador;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdTrabajador", idTrabajador);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TrabajadorDto
            {
                IdTrabajador = reader.GetInt32(reader.GetOrdinal("IdTrabajador")),
                CodigoTrabajador = reader.GetString(reader.GetOrdinal("CodigoTrabajador")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                DocumentoIdentidad = reader.GetString(reader.GetOrdinal("DocumentoIdentidad")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                TipoTrabajador = reader.GetString(reader.GetOrdinal("TipoTrabajador")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                Vehiculo = reader.IsDBNull(reader.GetOrdinal("Vehiculo")) ? null : reader.GetString(reader.GetOrdinal("Vehiculo")),
                IdUsuario = reader.IsDBNull(reader.GetOrdinal("IdUsuario")) ? null : reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
            };
        }

        return null;
    }

    public async Task<List<ConexionAguaDto>> ObtenerCandidatosAsignacionAsync(string tipoServicio, int cantidad, double puntoPartidaLon, double puntoPartidaLat)
    {
        var lista = new List<ConexionAguaDto>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerCandidatosAsignacion", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@TipoServicio", tipoServicio);
        cmd.Parameters.AddWithValue("@Cantidad", cantidad);
        cmd.Parameters.AddWithValue("@PuntoPartidaLon", puntoPartidaLon);
        cmd.Parameters.AddWithValue("@PuntoPartidaLat", puntoPartidaLat);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ConexionAguaDto
            {
                IdConexion = reader.GetInt32(reader.GetOrdinal("IdConexion")),
                CodigoConexion = reader.GetString(reader.GetOrdinal("CodigoConexion")),
                NumeroMedidor = reader.GetString(reader.GetOrdinal("NumeroMedidor")),
                CodigoLote = reader.IsDBNull(reader.GetOrdinal("CodigoLote")) ? null : reader.GetString(reader.GetOrdinal("CodigoLote")),
                CodigoManzana = reader.IsDBNull(reader.GetOrdinal("CodigoManzana")) ? null : reader.GetString(reader.GetOrdinal("CodigoManzana")),
                Titular = reader.GetString(reader.GetOrdinal("Titular")),
                Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                EstadoConexion = reader.GetString(reader.GetOrdinal("EstadoConexion")),
                MesesMora = reader.GetInt32(reader.GetOrdinal("MesesMora")),
                SaldoPendiente = reader.GetDecimal(reader.GetOrdinal("SaldoPendiente")),
                UltimaLectura = reader.IsDBNull(reader.GetOrdinal("UltimaLectura")) ? null : reader.GetDecimal(reader.GetOrdinal("UltimaLectura")),
                Longitud = reader.GetDouble(reader.GetOrdinal("Longitud")),
                Latitud = reader.GetDouble(reader.GetOrdinal("Latitud")),
                DistanciaAproximadaMetros = reader.IsDBNull(reader.GetOrdinal("DistanciaAproximadaMetros")) ? null : reader.GetDouble(reader.GetOrdinal("DistanciaAproximadaMetros")),
                Wkt = reader.IsDBNull(reader.GetOrdinal("WKT")) ? null : reader.GetString(reader.GetOrdinal("WKT"))
            });
        }

        return lista;
    }

    public async Task<int> CrearAsignacionAsync(Asignacion asignacion, List<AsignacionDetalle> detalles)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            const string sqlAsignacion = @"
                INSERT INTO dbo.Asignaciones 
                (CodigoAsignacion, IdTrabajador, TipoServicio, FechaAsignacion, Estado, CantidadPuntos, PuntoPartidaNombre, PuntoPartidaLon, PuntoPartidaLat, DistanciaTotalMetros, RutaOptimaWkt, Notas, UsuarioCreador, FechaCreacion)
                VALUES 
                (@CodigoAsignacion, @IdTrabajador, @TipoServicio, @FechaAsignacion, @Estado, @CantidadPuntos, @PuntoPartidaNombre, @PuntoPartidaLon, @PuntoPartidaLat, @DistanciaTotalMetros, @RutaOptimaWkt, @Notas, @UsuarioCreador, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmdAsig = new SqlCommand(sqlAsignacion, conn, tx);
            cmdAsig.Parameters.AddWithValue("@CodigoAsignacion", asignacion.CodigoAsignacion);
            cmdAsig.Parameters.AddWithValue("@IdTrabajador", asignacion.IdTrabajador);
            cmdAsig.Parameters.AddWithValue("@TipoServicio", asignacion.TipoServicio);
            cmdAsig.Parameters.AddWithValue("@FechaAsignacion", asignacion.FechaAsignacion);
            cmdAsig.Parameters.AddWithValue("@Estado", asignacion.Estado);
            cmdAsig.Parameters.AddWithValue("@CantidadPuntos", asignacion.CantidadPuntos);
            cmdAsig.Parameters.AddWithValue("@PuntoPartidaNombre", asignacion.PuntoPartidaNombre);
            cmdAsig.Parameters.AddWithValue("@PuntoPartidaLon", asignacion.PuntoPartidaLon);
            cmdAsig.Parameters.AddWithValue("@PuntoPartidaLat", asignacion.PuntoPartidaLat);
            cmdAsig.Parameters.AddWithValue("@DistanciaTotalMetros", asignacion.DistanciaTotalMetros);
            cmdAsig.Parameters.AddWithValue("@RutaOptimaWkt", (object?)asignacion.RutaOptimaWkt ?? DBNull.Value);
            cmdAsig.Parameters.AddWithValue("@Notas", (object?)asignacion.Notas ?? DBNull.Value);
            cmdAsig.Parameters.AddWithValue("@UsuarioCreador", (object?)asignacion.UsuarioCreador ?? DBNull.Value);

            object? scalarResult = await cmdAsig.ExecuteScalarAsync();
            int idAsignacion = Convert.ToInt32(scalarResult);

            const string sqlDetalle = @"
                INSERT INTO dbo.AsignacionDetalle
                (IdAsignacion, IdConexion, OrdenSecuencia, DistanciaDesdeAnteriorM, Estado, LecturaAnterior)
                VALUES
                (@IdAsignacion, @IdConexion, @OrdenSecuencia, @DistanciaDesdeAnteriorM, @Estado, @LecturaAnterior);";

            foreach (var d in detalles)
            {
                using var cmdDet = new SqlCommand(sqlDetalle, conn, tx);
                cmdDet.Parameters.AddWithValue("@IdAsignacion", idAsignacion);
                cmdDet.Parameters.AddWithValue("@IdConexion", d.IdConexion);
                cmdDet.Parameters.AddWithValue("@OrdenSecuencia", d.OrdenSecuencia);
                cmdDet.Parameters.AddWithValue("@DistanciaDesdeAnteriorM", d.DistanciaDesdeAnteriorM);
                cmdDet.Parameters.AddWithValue("@Estado", d.Estado);
                cmdDet.Parameters.AddWithValue("@LecturaAnterior", (object?)d.LecturaAnterior ?? DBNull.Value);
                await cmdDet.ExecuteNonQueryAsync();
            }

            // Actualizar estado del trabajador a EnRuta
            const string sqlTrabajador = @"
                UPDATE dbo.Trabajadores SET Estado = 'EnRuta' WHERE IdTrabajador = @IdTrabajador;
                UPDATE dbo.MonitoreoTrabajador SET IdAsignacionActiva = @IdAsignacion WHERE IdTrabajador = @IdTrabajador;";

            using var cmdTrab = new SqlCommand(sqlTrabajador, conn, tx);
            cmdTrab.Parameters.AddWithValue("@IdTrabajador", asignacion.IdTrabajador);
            cmdTrab.Parameters.AddWithValue("@IdAsignacion", idAsignacion);
            await cmdTrab.ExecuteNonQueryAsync();

            tx.Commit();
            return idAsignacion;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<AsignacionDto>> ObtenerAsignacionesAsync(string? estado = null, int? idTrabajador = null)
    {
        var lista = new List<AsignacionDto>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();

        const string sql = @"
            SELECT 
                a.IdAsignacion, a.CodigoAsignacion, a.IdTrabajador, t.NombreCompleto AS NombreTrabajador,
                t.TipoTrabajador, a.FechaAsignacion, a.TipoServicio, a.Estado, a.CantidadPuntos,
                a.PuntoPartidaNombre, a.PuntoPartidaLon, a.PuntoPartidaLat,
                a.DistanciaTotalMetros, a.RutaOptimaWkt, a.FechaInicio, a.FechaFinalizacion, a.Notas,
                (SELECT COUNT(*) FROM dbo.AsignacionDetalle ad WHERE ad.IdAsignacion = a.IdAsignacion AND ad.Estado = 'Ejecutado') AS PuntosCompletados
            FROM dbo.Asignaciones a
            INNER JOIN dbo.Trabajadores t ON a.IdTrabajador = t.IdTrabajador
            WHERE (@Estado IS NULL OR a.Estado = @Estado)
              AND (@IdTrabajador IS NULL OR a.IdTrabajador = @IdTrabajador)
            ORDER BY a.IdAsignacion DESC;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Estado", (object?)estado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IdTrabajador", (object?)idTrabajador ?? DBNull.Value);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new AsignacionDto
            {
                IdAsignacion = reader.GetInt32(reader.GetOrdinal("IdAsignacion")),
                CodigoAsignacion = reader.GetString(reader.GetOrdinal("CodigoAsignacion")),
                IdTrabajador = reader.GetInt32(reader.GetOrdinal("IdTrabajador")),
                NombreTrabajador = reader.GetString(reader.GetOrdinal("NombreTrabajador")),
                TipoTrabajador = reader.GetString(reader.GetOrdinal("TipoTrabajador")),
                FechaAsignacion = reader.GetDateTime(reader.GetOrdinal("FechaAsignacion")),
                TipoServicio = reader.GetString(reader.GetOrdinal("TipoServicio")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                CantidadPuntos = reader.GetInt32(reader.GetOrdinal("CantidadPuntos")),
                PuntosCompletados = reader.GetInt32(reader.GetOrdinal("PuntosCompletados")),
                PuntoPartidaNombre = reader.GetString(reader.GetOrdinal("PuntoPartidaNombre")),
                PuntoPartidaLon = reader.GetDouble(reader.GetOrdinal("PuntoPartidaLon")),
                PuntoPartidaLat = reader.GetDouble(reader.GetOrdinal("PuntoPartidaLat")),
                DistanciaTotalMetros = reader.GetDecimal(reader.GetOrdinal("DistanciaTotalMetros")),
                RutaOptimaWkt = reader.IsDBNull(reader.GetOrdinal("RutaOptimaWkt")) ? null : reader.GetString(reader.GetOrdinal("RutaOptimaWkt")),
                FechaInicio = reader.IsDBNull(reader.GetOrdinal("FechaInicio")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaInicio")),
                FechaFinalizacion = reader.IsDBNull(reader.GetOrdinal("FechaFinalizacion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaFinalizacion")),
                Notas = reader.IsDBNull(reader.GetOrdinal("Notas")) ? null : reader.GetString(reader.GetOrdinal("Notas"))
            });
        }

        return lista;
    }

    public async Task<AsignacionDto?> ObtenerAsignacionPorIdAsync(int idAsignacion)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();

        const string sqlCabecera = @"
            SELECT 
                a.IdAsignacion, a.CodigoAsignacion, a.IdTrabajador, t.NombreCompleto AS NombreTrabajador,
                t.TipoTrabajador, a.FechaAsignacion, a.TipoServicio, a.Estado, a.CantidadPuntos,
                a.PuntoPartidaNombre, a.PuntoPartidaLon, a.PuntoPartidaLat,
                a.DistanciaTotalMetros, a.RutaOptimaWkt, a.FechaInicio, a.FechaFinalizacion, a.Notas,
                (SELECT COUNT(*) FROM dbo.AsignacionDetalle ad WHERE ad.IdAsignacion = a.IdAsignacion AND ad.Estado = 'Ejecutado') AS PuntosCompletados
            FROM dbo.Asignaciones a
            INNER JOIN dbo.Trabajadores t ON a.IdTrabajador = t.IdTrabajador
            WHERE a.IdAsignacion = @IdAsignacion;";

        AsignacionDto? asig = null;
        using (var cmd = new SqlCommand(sqlCabecera, conn))
        {
            cmd.Parameters.AddWithValue("@IdAsignacion", idAsignacion);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                asig = new AsignacionDto
                {
                    IdAsignacion = reader.GetInt32(reader.GetOrdinal("IdAsignacion")),
                    CodigoAsignacion = reader.GetString(reader.GetOrdinal("CodigoAsignacion")),
                    IdTrabajador = reader.GetInt32(reader.GetOrdinal("IdTrabajador")),
                    NombreTrabajador = reader.GetString(reader.GetOrdinal("NombreTrabajador")),
                    TipoTrabajador = reader.GetString(reader.GetOrdinal("TipoTrabajador")),
                    FechaAsignacion = reader.GetDateTime(reader.GetOrdinal("FechaAsignacion")),
                    TipoServicio = reader.GetString(reader.GetOrdinal("TipoServicio")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado")),
                    CantidadPuntos = reader.GetInt32(reader.GetOrdinal("CantidadPuntos")),
                    PuntosCompletados = reader.GetInt32(reader.GetOrdinal("PuntosCompletados")),
                    PuntoPartidaNombre = reader.GetString(reader.GetOrdinal("PuntoPartidaNombre")),
                    PuntoPartidaLon = reader.GetDouble(reader.GetOrdinal("PuntoPartidaLon")),
                    PuntoPartidaLat = reader.GetDouble(reader.GetOrdinal("PuntoPartidaLat")),
                    DistanciaTotalMetros = reader.GetDecimal(reader.GetOrdinal("DistanciaTotalMetros")),
                    RutaOptimaWkt = reader.IsDBNull(reader.GetOrdinal("RutaOptimaWkt")) ? null : reader.GetString(reader.GetOrdinal("RutaOptimaWkt")),
                    FechaInicio = reader.IsDBNull(reader.GetOrdinal("FechaInicio")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaInicio")),
                    FechaFinalizacion = reader.IsDBNull(reader.GetOrdinal("FechaFinalizacion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaFinalizacion")),
                    Notas = reader.IsDBNull(reader.GetOrdinal("Notas")) ? null : reader.GetString(reader.GetOrdinal("Notas"))
                };
            }
        }

        if (asig == null) return null;

        const string sqlDetalles = @"
            SELECT 
                ad.IdDetalle, ad.IdAsignacion, ad.IdConexion, c.CodigoConexion, c.NumeroMedidor,
                c.CodigoLote, c.CodigoManzana, c.Titular, c.Direccion,
                ad.OrdenSecuencia, ad.DistanciaDesdeAnteriorM, ad.Estado,
                ad.LecturaAnterior, ad.LecturaActual, ad.NumeroMedidorReportado,
                ad.FotoMedidorUrl, ad.Observacion, ad.FechaHoraEjecucion,
                ad.GpsLatEjecucion, ad.GpsLonEjecucion, ad.GpsPrecisionM,
                ad.DistanciaValidacionGpsM, ad.ValidacionGpsExitosa,
                c.Longitud, c.Latitud
            FROM dbo.AsignacionDetalle ad
            INNER JOIN dbo.ConexionesAgua c ON ad.IdConexion = c.IdConexion
            WHERE ad.IdAsignacion = @IdAsignacion
            ORDER BY ad.OrdenSecuencia ASC;";

        using (var cmd = new SqlCommand(sqlDetalles, conn))
        {
            cmd.Parameters.AddWithValue("@IdAsignacion", idAsignacion);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                asig.Puntos.Add(new AsignacionPuntoDto
                {
                    IdDetalle = reader.GetInt32(reader.GetOrdinal("IdDetalle")),
                    IdAsignacion = reader.GetInt32(reader.GetOrdinal("IdAsignacion")),
                    IdConexion = reader.GetInt32(reader.GetOrdinal("IdConexion")),
                    CodigoConexion = reader.GetString(reader.GetOrdinal("CodigoConexion")),
                    NumeroMedidor = reader.GetString(reader.GetOrdinal("NumeroMedidor")),
                    CodigoLote = reader.IsDBNull(reader.GetOrdinal("CodigoLote")) ? null : reader.GetString(reader.GetOrdinal("CodigoLote")),
                    CodigoManzana = reader.IsDBNull(reader.GetOrdinal("CodigoManzana")) ? null : reader.GetString(reader.GetOrdinal("CodigoManzana")),
                    Titular = reader.GetString(reader.GetOrdinal("Titular")),
                    Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                    OrdenSecuencia = reader.GetInt32(reader.GetOrdinal("OrdenSecuencia")),
                    DistanciaDesdeAnteriorM = reader.GetDecimal(reader.GetOrdinal("DistanciaDesdeAnteriorM")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado")),
                    LecturaAnterior = reader.IsDBNull(reader.GetOrdinal("LecturaAnterior")) ? null : reader.GetDecimal(reader.GetOrdinal("LecturaAnterior")),
                    LecturaActual = reader.IsDBNull(reader.GetOrdinal("LecturaActual")) ? null : reader.GetDecimal(reader.GetOrdinal("LecturaActual")),
                    NumeroMedidorReportado = reader.IsDBNull(reader.GetOrdinal("NumeroMedidorReportado")) ? null : reader.GetString(reader.GetOrdinal("NumeroMedidorReportado")),
                    FotoMedidorUrl = reader.IsDBNull(reader.GetOrdinal("FotoMedidorUrl")) ? null : reader.GetString(reader.GetOrdinal("FotoMedidorUrl")),
                    Observacion = reader.IsDBNull(reader.GetOrdinal("Observacion")) ? null : reader.GetString(reader.GetOrdinal("Observacion")),
                    FechaHoraEjecucion = reader.IsDBNull(reader.GetOrdinal("FechaHoraEjecucion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaHoraEjecucion")),
                    GpsLatEjecucion = reader.IsDBNull(reader.GetOrdinal("GpsLatEjecucion")) ? null : reader.GetDouble(reader.GetOrdinal("GpsLatEjecucion")),
                    GpsLonEjecucion = reader.IsDBNull(reader.GetOrdinal("GpsLonEjecucion")) ? null : reader.GetDouble(reader.GetOrdinal("GpsLonEjecucion")),
                    GpsPrecisionM = reader.IsDBNull(reader.GetOrdinal("GpsPrecisionM")) ? null : reader.GetDouble(reader.GetOrdinal("GpsPrecisionM")),
                    DistanciaValidacionGpsM = reader.IsDBNull(reader.GetOrdinal("DistanciaValidacionGpsM")) ? null : reader.GetDecimal(reader.GetOrdinal("DistanciaValidacionGpsM")),
                    ValidacionGpsExitosa = reader.IsDBNull(reader.GetOrdinal("ValidacionGpsExitosa")) ? null : reader.GetBoolean(reader.GetOrdinal("ValidacionGpsExitosa")),
                    Longitud = reader.GetDouble(reader.GetOrdinal("Longitud")),
                    Latitud = reader.GetDouble(reader.GetOrdinal("Latitud"))
                });
            }
        }

        return asig;
    }

    public async Task<RegistroEjecucionCampoResponseDto> RegistrarEjecucionCampoAsync(RegistroEjecucionCampoRequestDto request)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_RegistrarEjecucionCampo", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdDetalle", request.IdDetalle);
        cmd.Parameters.AddWithValue("@EstadoResultado", request.EstadoResultado);
        cmd.Parameters.AddWithValue("@LecturaActual", (object?)request.LecturaActual ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NumeroMedidorReportado", (object?)request.NumeroMedidorReportado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FotoMedidorUrl", (object?)request.FotoMedidorUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Observacion", (object?)request.Observacion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GpsLat", request.GpsLat);
        cmd.Parameters.AddWithValue("@GpsLon", request.GpsLon);
        cmd.Parameters.AddWithValue("@GpsPrecisionM", request.GpsPrecisionM);
        cmd.Parameters.AddWithValue("@ToleranciaMetrosMax", request.ToleranciaMetrosMax);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            int ordDist;
            try { ordDist = reader.GetOrdinal("DistanciaMetros"); }
            catch { ordDist = reader.GetOrdinal("DistanciaMetrosCalculada"); }

            int ordEst = -1;
            try { ordEst = reader.GetOrdinal("EstadoFinal"); } catch { }

            return new RegistroEjecucionCampoResponseDto
            {
                Exito = true,
                Mensaje = reader.GetString(reader.GetOrdinal("Mensaje")),
                DistanciaMetros = reader.GetDecimal(ordDist),
                ValidacionGpsExitosa = reader.GetBoolean(reader.GetOrdinal("ValidacionGpsExitosa")),
                EstadoFinal = ordEst >= 0 && !reader.IsDBNull(ordEst) ? reader.GetString(ordEst) : request.EstadoResultado
            };
        }

        return new RegistroEjecucionCampoResponseDto { Exito = false, Mensaje = "No se obtuvo respuesta del procedimiento." };
    }

    public async Task<bool> ActualizarUbicacionMonitoreoAsync(ActualizarUbicacionGpsRequestDto request)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ActualizarUbicacionMonitoreo", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdTrabajador", request.IdTrabajador);
        cmd.Parameters.AddWithValue("@Latitud", request.Latitud);
        cmd.Parameters.AddWithValue("@Longitud", request.Longitud);
        cmd.Parameters.AddWithValue("@VelocidadKmH", request.VelocidadKmH);
        cmd.Parameters.AddWithValue("@Rumbo", (object?)request.Rumbo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PrecisionM", request.PrecisionM);
        cmd.Parameters.AddWithValue("@BateriaPorcentaje", request.BateriaPorcentaje);

        await cmd.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<MonitoreoOperarioDto>> ObtenerMonitoreoOperariosAsync()
    {
        var lista = new List<MonitoreoOperarioDto>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerMonitoreoOperarios", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new MonitoreoOperarioDto
            {
                IdMonitoreo = reader.GetInt32(reader.GetOrdinal("IdMonitoreo")),
                IdTrabajador = reader.GetInt32(reader.GetOrdinal("IdTrabajador")),
                CodigoTrabajador = reader.GetString(reader.GetOrdinal("CodigoTrabajador")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                TipoTrabajador = reader.GetString(reader.GetOrdinal("TipoTrabajador")),
                Vehiculo = reader.IsDBNull(reader.GetOrdinal("Vehiculo")) ? null : reader.GetString(reader.GetOrdinal("Vehiculo")),
                EstadoOperario = reader.GetString(reader.GetOrdinal("EstadoOperario")),
                Latitud = reader.GetDouble(reader.GetOrdinal("Latitud")),
                Longitud = reader.GetDouble(reader.GetOrdinal("Longitud")),
                VelocidadKmH = reader.GetDouble(reader.GetOrdinal("VelocidadKmH")),
                Rumbo = reader.IsDBNull(reader.GetOrdinal("Rumbo")) ? null : reader.GetDouble(reader.GetOrdinal("Rumbo")),
                PrecisionM = reader.GetDouble(reader.GetOrdinal("PrecisionM")),
                BateriaPorcentaje = reader.GetInt32(reader.GetOrdinal("BateriaPorcentaje")),
                UltimaActualizacion = reader.GetDateTime(reader.GetOrdinal("UltimaActualizacion")),
                EnLinea = reader.GetBoolean(reader.GetOrdinal("EnLinea")),
                CodigoAsignacion = reader.IsDBNull(reader.GetOrdinal("CodigoAsignacion")) ? null : reader.GetString(reader.GetOrdinal("CodigoAsignacion")),
                TipoServicio = reader.IsDBNull(reader.GetOrdinal("TipoServicio")) ? null : reader.GetString(reader.GetOrdinal("TipoServicio")),
                CantidadPuntos = reader.IsDBNull(reader.GetOrdinal("CantidadPuntos")) ? null : reader.GetInt32(reader.GetOrdinal("CantidadPuntos")),
                PuntosCompletados = reader.IsDBNull(reader.GetOrdinal("PuntosCompletados")) ? null : reader.GetInt32(reader.GetOrdinal("PuntosCompletados")),
                SegundosDesdeUltimaReporte = reader.IsDBNull(reader.GetOrdinal("SegundosDesdeUltimaReporte")) ? null : reader.GetInt32(reader.GetOrdinal("SegundosDesdeUltimaReporte"))
            });
        }

        return lista;
    }

    public async Task<List<Via>> ObtenerRedVialAsync()
    {
        var lista = new List<Via>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();

        const string sql = @"
            SELECT IdVia, CodigoVia, Nombre, TipoVia, Jerarquia, Sentido, AnchoViaM, Pavimento, LongitudM, OrigenId, Geometria.STAsText() AS WktGeometria
            FROM dbo.Vias;";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Via
            {
                IdVia = reader.GetInt32(reader.GetOrdinal("IdVia")),
                CodigoVia = reader.GetString(reader.GetOrdinal("CodigoVia")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                TipoVia = reader.IsDBNull(reader.GetOrdinal("TipoVia")) ? null : reader.GetString(reader.GetOrdinal("TipoVia")),
                Jerarquia = reader.IsDBNull(reader.GetOrdinal("Jerarquia")) ? null : reader.GetString(reader.GetOrdinal("Jerarquia")),
                Sentido = reader.IsDBNull(reader.GetOrdinal("Sentido")) ? null : reader.GetString(reader.GetOrdinal("Sentido")),
                AnchoViaM = reader.IsDBNull(reader.GetOrdinal("AnchoViaM")) ? null : reader.GetDecimal(reader.GetOrdinal("AnchoViaM")),
                Pavimento = reader.IsDBNull(reader.GetOrdinal("Pavimento")) ? null : reader.GetString(reader.GetOrdinal("Pavimento")),
                LongitudM = reader.IsDBNull(reader.GetOrdinal("LongitudM")) ? null : reader.GetDecimal(reader.GetOrdinal("LongitudM")),
                OrigenId = reader.IsDBNull(reader.GetOrdinal("OrigenId")) ? null : reader.GetString(reader.GetOrdinal("OrigenId")),
                WktGeometria = reader.IsDBNull(reader.GetOrdinal("WktGeometria")) ? string.Empty : reader.GetString(reader.GetOrdinal("WktGeometria"))
            });
        }

        return lista;
    }
}
