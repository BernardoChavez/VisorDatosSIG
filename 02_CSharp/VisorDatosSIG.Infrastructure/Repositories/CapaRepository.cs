#nullable enable
using System.Data;
using System.Data.SqlClient;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.ValueObjects;
using VisorDatosSIG.Infrastructure.Data;

namespace VisorDatosSIG.Infrastructure.Repositories;

public class CapaRepository : ICapaRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public CapaRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<CatalogoCapa>> ObtenerCatalogoCapasAsync()
    {
        var lista = new List<CatalogoCapa>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerCapasCatalogo", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new CatalogoCapa
            {
                IdCapa = reader.GetInt32(reader.GetOrdinal("IdCapa")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
                Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString(reader.GetOrdinal("Descripcion")),
                TipoGeometria = reader.GetString(reader.GetOrdinal("TipoGeometria")),
                ColorBorde = reader.GetString(reader.GetOrdinal("ColorBorde")),
                ColorFondo = reader.GetString(reader.GetOrdinal("ColorFondo")),
                Opacidad = reader.GetDecimal(reader.GetOrdinal("Opacidad")),
                GrosorBorde = reader.GetInt32(reader.GetOrdinal("GrosorBorde")),
                OrdenVisualizacion = reader.GetInt32(reader.GetOrdinal("OrdenVisualizacion")),
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                Icono = reader.IsDBNull(reader.GetOrdinal("Icono")) ? null : reader.GetString(reader.GetOrdinal("Icono")),
                CamposVisibles = reader.IsDBNull(reader.GetOrdinal("CamposVisibles")) ? null : reader.GetString(reader.GetOrdinal("CamposVisibles"))
            });
        }

        return lista;
    }

    public async Task<CatalogoCapa?> ObtenerCapaPorNombreAsync(string nombreCapa)
    {
        var catalogo = await ObtenerCatalogoCapasAsync();
        return catalogo.FirstOrDefault(c => c.Nombre.Equals(nombreCapa, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Manzana>> ObtenerManzanasPorBBoxAsync(BoundingBox? bbox, int limite = 5000)
    {
        var lista = new List<Manzana>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ConsultarManzanasPorBBox", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        cmd.Parameters.AddWithValue("@MinLon", bbox != null ? (object)bbox.MinLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MinLat", bbox != null ? (object)bbox.MinLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLon", bbox != null ? (object)bbox.MaxLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLat", bbox != null ? (object)bbox.MaxLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@Limite", limite);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Manzana
            {
                IdManzana = reader.GetInt32(reader.GetOrdinal("IdManzana")),
                CodigoManzana = reader.IsDBNull(reader.GetOrdinal("CodigoManzana")) ? "" : reader.GetString(reader.GetOrdinal("CodigoManzana")),
                Sector = reader.IsDBNull(reader.GetOrdinal("Sector")) ? null : reader.GetString(reader.GetOrdinal("Sector")),
                Barrio = reader.IsDBNull(reader.GetOrdinal("Barrio")) ? null : reader.GetString(reader.GetOrdinal("Barrio")),
                AreaM2 = reader.IsDBNull(reader.GetOrdinal("AreaM2")) ? null : reader.GetDecimal(reader.GetOrdinal("AreaM2")),
                PerimetroM = reader.IsDBNull(reader.GetOrdinal("PerimetroM")) ? null : reader.GetDecimal(reader.GetOrdinal("PerimetroM")),
                OrigenId = reader.IsDBNull(reader.GetOrdinal("OrigenId")) ? null : reader.GetString(reader.GetOrdinal("OrigenId")),
                WktGeometria = reader.IsDBNull(reader.GetOrdinal("WKT")) ? "" : reader.GetString(reader.GetOrdinal("WKT")),
                LonCentro = reader.IsDBNull(reader.GetOrdinal("LonCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LonCentro")),
                LatCentro = reader.IsDBNull(reader.GetOrdinal("LatCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LatCentro"))
            });
        }

        return lista;
    }

    public async Task<List<Lote>> ObtenerLotesPorBBoxAsync(BoundingBox? bbox, int limite = 5000)
    {
        var lista = new List<Lote>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ConsultarLotesPorBBox", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        cmd.Parameters.AddWithValue("@MinLon", bbox != null ? (object)bbox.MinLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MinLat", bbox != null ? (object)bbox.MinLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLon", bbox != null ? (object)bbox.MaxLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLat", bbox != null ? (object)bbox.MaxLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@Limite", limite);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Lote
            {
                IdLote = reader.GetInt32(reader.GetOrdinal("IdLote")),
                CodigoLote = reader.IsDBNull(reader.GetOrdinal("CodigoLote")) ? "" : reader.GetString(reader.GetOrdinal("CodigoLote")),
                CodigoManzana = reader.IsDBNull(reader.GetOrdinal("CodigoManzana")) ? null : reader.GetString(reader.GetOrdinal("CodigoManzana")),
                NumeroPredial = reader.IsDBNull(reader.GetOrdinal("NumeroPredial")) ? null : reader.GetString(reader.GetOrdinal("NumeroPredial")),
                UsoSuelo = reader.IsDBNull(reader.GetOrdinal("UsoSuelo")) ? null : reader.GetString(reader.GetOrdinal("UsoSuelo")),
                AreaTerrenoM2 = reader.IsDBNull(reader.GetOrdinal("AreaTerrenoM2")) ? null : reader.GetDecimal(reader.GetOrdinal("AreaTerrenoM2")),
                FrenteM = reader.IsDBNull(reader.GetOrdinal("FrenteM")) ? null : reader.GetDecimal(reader.GetOrdinal("FrenteM")),
                FondoM = reader.IsDBNull(reader.GetOrdinal("FondoM")) ? null : reader.GetDecimal(reader.GetOrdinal("FondoM")),
                Titular = reader.IsDBNull(reader.GetOrdinal("Titular")) ? null : reader.GetString(reader.GetOrdinal("Titular")),
                Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString(reader.GetOrdinal("Direccion")),
                OrigenId = reader.IsDBNull(reader.GetOrdinal("OrigenId")) ? null : reader.GetString(reader.GetOrdinal("OrigenId")),
                WktGeometria = reader.IsDBNull(reader.GetOrdinal("WKT")) ? "" : reader.GetString(reader.GetOrdinal("WKT")),
                LonCentro = reader.IsDBNull(reader.GetOrdinal("LonCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LonCentro")),
                LatCentro = reader.IsDBNull(reader.GetOrdinal("LatCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LatCentro"))
            });
        }

        return lista;
    }

    public async Task<List<CodigoFijo>> ObtenerCodigosFijosPorBBoxAsync(BoundingBox? bbox, int limite = 5000)
    {
        var lista = new List<CodigoFijo>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ConsultarCodigosFijosPorBBox", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        cmd.Parameters.AddWithValue("@MinLon", bbox != null ? (object)bbox.MinLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MinLat", bbox != null ? (object)bbox.MinLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLon", bbox != null ? (object)bbox.MaxLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLat", bbox != null ? (object)bbox.MaxLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@Limite", limite);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new CodigoFijo
            {
                IdCodigoFijo = reader.GetInt32(reader.GetOrdinal("IdCodigoFijo")),
                CodigoFijoValor = reader.IsDBNull(reader.GetOrdinal("CodigoFijo")) ? "" : reader.GetString(reader.GetOrdinal("CodigoFijo")),
                TipoPunto = reader.IsDBNull(reader.GetOrdinal("TipoPunto")) ? null : reader.GetString(reader.GetOrdinal("TipoPunto")),
                Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString(reader.GetOrdinal("Descripcion")),
                Referencia = reader.IsDBNull(reader.GetOrdinal("Referencia")) ? null : reader.GetString(reader.GetOrdinal("Referencia")),
                CotaZ = reader.IsDBNull(reader.GetOrdinal("CotaZ")) ? null : reader.GetDecimal(reader.GetOrdinal("CotaZ")),
                OrigenId = reader.IsDBNull(reader.GetOrdinal("OrigenId")) ? null : reader.GetString(reader.GetOrdinal("OrigenId")),
                WktGeometria = reader.IsDBNull(reader.GetOrdinal("WKT")) ? "" : reader.GetString(reader.GetOrdinal("WKT")),
                LonCentro = reader.IsDBNull(reader.GetOrdinal("LonCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LonCentro")),
                LatCentro = reader.IsDBNull(reader.GetOrdinal("LatCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LatCentro"))
            });
        }

        return lista;
    }

    public async Task<List<Via>> ObtenerViasPorBBoxAsync(BoundingBox? bbox, int limite = 5000)
    {
        var lista = new List<Via>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ConsultarViasPorBBox", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        cmd.Parameters.AddWithValue("@MinLon", bbox != null ? (object)bbox.MinLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MinLat", bbox != null ? (object)bbox.MinLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLon", bbox != null ? (object)bbox.MaxLon : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxLat", bbox != null ? (object)bbox.MaxLat : DBNull.Value);
        cmd.Parameters.AddWithValue("@Limite", limite);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Via
            {
                IdVia = reader.GetInt32(reader.GetOrdinal("IdVia")),
                CodigoVia = reader.IsDBNull(reader.GetOrdinal("CodigoVia")) ? "" : reader.GetString(reader.GetOrdinal("CodigoVia")),
                Nombre = reader.IsDBNull(reader.GetOrdinal("Nombre")) ? "" : reader.GetString(reader.GetOrdinal("Nombre")),
                TipoVia = reader.IsDBNull(reader.GetOrdinal("TipoVia")) ? null : reader.GetString(reader.GetOrdinal("TipoVia")),
                Jerarquia = reader.IsDBNull(reader.GetOrdinal("Jerarquia")) ? null : reader.GetString(reader.GetOrdinal("Jerarquia")),
                Sentido = reader.IsDBNull(reader.GetOrdinal("Sentido")) ? null : reader.GetString(reader.GetOrdinal("Sentido")),
                AnchoViaM = reader.IsDBNull(reader.GetOrdinal("AnchoViaM")) ? null : reader.GetDecimal(reader.GetOrdinal("AnchoViaM")),
                Pavimento = reader.IsDBNull(reader.GetOrdinal("Pavimento")) ? null : reader.GetString(reader.GetOrdinal("Pavimento")),
                LongitudM = reader.IsDBNull(reader.GetOrdinal("LongitudM")) ? null : reader.GetDecimal(reader.GetOrdinal("LongitudM")),
                OrigenId = reader.IsDBNull(reader.GetOrdinal("OrigenId")) ? null : reader.GetString(reader.GetOrdinal("OrigenId")),
                WktGeometria = reader.IsDBNull(reader.GetOrdinal("WKT")) ? "" : reader.GetString(reader.GetOrdinal("WKT")),
                LonCentro = reader.IsDBNull(reader.GetOrdinal("LonCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LonCentro")),
                LatCentro = reader.IsDBNull(reader.GetOrdinal("LatCentro")) ? null : reader.GetDouble(reader.GetOrdinal("LatCentro"))
            });
        }

        return lista;
    }

    public async Task<object?> ObtenerEntidadPorIdAsync(string nombreCapa, int idEntidad)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();

        switch (nombreCapa.Trim().ToLowerInvariant())
        {
            case "lotes":
                using (var cmd = new SqlCommand("SELECT IdLote, CodigoLote, CodigoManzana, NumeroPredial, UsoSuelo, AreaTerrenoM2, FrenteM, FondoM, Titular, Direccion, OrigenId, Geometria.STAsText() AS WKT, Geometria.STCentroid().STX AS LonCentro, Geometria.STCentroid().STY AS LatCentro, FechaRegistro FROM dbo.Lotes WHERE IdLote = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEntidad);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new Lote
                        {
                            IdLote = reader.GetInt32(0),
                            CodigoLote = reader.GetString(1),
                            CodigoManzana = reader.IsDBNull(2) ? null : reader.GetString(2),
                            NumeroPredial = reader.IsDBNull(3) ? null : reader.GetString(3),
                            UsoSuelo = reader.IsDBNull(4) ? null : reader.GetString(4),
                            AreaTerrenoM2 = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                            FrenteM = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                            FondoM = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            Titular = reader.IsDBNull(8) ? null : reader.GetString(8),
                            Direccion = reader.IsDBNull(9) ? null : reader.GetString(9),
                            OrigenId = reader.IsDBNull(10) ? null : reader.GetString(10),
                            WktGeometria = reader.GetString(11),
                            LonCentro = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                            LatCentro = reader.IsDBNull(13) ? null : reader.GetDouble(13),
                            FechaRegistro = reader.GetDateTime(14)
                        };
                    }
                }
                break;

            case "manzanas":
                using (var cmd = new SqlCommand("SELECT IdManzana, CodigoManzana, Sector, Barrio, AreaM2, PerimetroM, OrigenId, Geometria.STAsText() AS WKT, Geometria.STCentroid().STX AS LonCentro, Geometria.STCentroid().STY AS LatCentro, FechaRegistro FROM dbo.Manzanas WHERE IdManzana = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEntidad);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new Manzana
                        {
                            IdManzana = reader.GetInt32(0),
                            CodigoManzana = reader.GetString(1),
                            Sector = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Barrio = reader.IsDBNull(3) ? null : reader.GetString(3),
                            AreaM2 = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                            PerimetroM = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                            OrigenId = reader.IsDBNull(6) ? null : reader.GetString(6),
                            WktGeometria = reader.GetString(7),
                            LonCentro = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                            LatCentro = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                            FechaRegistro = reader.GetDateTime(10)
                        };
                    }
                }
                break;

            case "codigosfijos":
                using (var cmd = new SqlCommand("SELECT IdCodigoFijo, CodigoFijo, TipoPunto, Descripcion, Referencia, CotaZ, OrigenId, Geometria.STAsText() AS WKT, Geometria.STX AS LonCentro, Geometria.STY AS LatCentro, FechaRegistro FROM dbo.CodigosFijos WHERE IdCodigoFijo = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEntidad);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new CodigoFijo
                        {
                            IdCodigoFijo = reader.GetInt32(0),
                            CodigoFijoValor = reader.GetString(1),
                            TipoPunto = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Descripcion = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Referencia = reader.IsDBNull(4) ? null : reader.GetString(4),
                            CotaZ = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                            OrigenId = reader.IsDBNull(6) ? null : reader.GetString(6),
                            WktGeometria = reader.GetString(7),
                            LonCentro = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                            LatCentro = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                            FechaRegistro = reader.GetDateTime(10)
                        };
                    }
                }
                break;

            case "vias":
                using (var cmd = new SqlCommand("SELECT IdVia, CodigoVia, Nombre, TipoVia, Jerarquia, Sentido, AnchoViaM, Pavimento, LongitudM, OrigenId, Geometria.STAsText() AS WKT, Geometria.STCentroid().STX AS LonCentro, Geometria.STCentroid().STY AS LatCentro, FechaRegistro FROM dbo.Vias WHERE IdVia = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEntidad);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new Via
                        {
                            IdVia = reader.GetInt32(0),
                            CodigoVia = reader.GetString(1),
                            Nombre = reader.GetString(2),
                            TipoVia = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Jerarquia = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Sentido = reader.IsDBNull(5) ? null : reader.GetString(5),
                            AnchoViaM = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                            Pavimento = reader.IsDBNull(7) ? null : reader.GetString(7),
                            LongitudM = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                            OrigenId = reader.IsDBNull(9) ? null : reader.GetString(9),
                            WktGeometria = reader.GetString(10),
                            LonCentro = reader.IsDBNull(11) ? null : reader.GetDouble(11),
                            LatCentro = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                            FechaRegistro = reader.GetDateTime(13)
                        };
                    }
                }
                break;
        }

        return null;
    }
}
