using System.Collections.Concurrent;
using VisorDatosSIG.Application.DTOs.Capas;
using VisorDatosSIG.Application.DTOs.GeoJson;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Application.Services;

public class CapaService : ICapaService
{
    private static readonly ConcurrentDictionary<string, (GeoJsonFeatureCollection Data, DateTime ExpirationUtc)> _geoJsonCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, (List<CapaCatalogoDto> Data, DateTime ExpirationUtc)> _catalogoCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(20);

    private readonly ICapaRepository _capaRepository;
    private readonly IAguaRepository? _aguaRepository;

    public CapaService(ICapaRepository capaRepository, IAguaRepository? aguaRepository = null)
    {
        _capaRepository = capaRepository;
        _aguaRepository = aguaRepository;
    }

    public void InvalidarCache(string? nombreCapa = null)
    {
        InvalidarCacheStatic(nombreCapa);
    }

    public void InvalidarCache(VisorDatosSIG.Domain.Enums.TipoCapa tipoCapa)
    {
        InvalidarCacheStatic(tipoCapa.ToString());
    }

    public static void InvalidarCacheStatic(string? nombreCapa = null)
    {
        if (string.IsNullOrWhiteSpace(nombreCapa))
        {
            _geoJsonCache.Clear();
            _catalogoCache.Clear();
        }
        else
        {
            _geoJsonCache.TryRemove(nombreCapa.Trim().ToLowerInvariant(), out _);
            _catalogoCache.Clear();
        }
    }

    public static void InvalidarCacheStatic(VisorDatosSIG.Domain.Enums.TipoCapa tipoCapa)
    {
        InvalidarCacheStatic(tipoCapa.ToString());
    }

    public async Task<List<CapaCatalogoDto>> ObtenerCatalogoCapasAsync()
    {
        const string catKey = "catalogo_activo";
        if (_catalogoCache.TryGetValue(catKey, out var cached) && cached.ExpirationUtc > DateTime.UtcNow)
        {
            return cached.Data;
        }

        var capas = await _capaRepository.ObtenerCatalogoCapasAsync();
        var resultado = capas.Select(c => new CapaCatalogoDto
        {
            IdCapa = c.IdCapa,
            Nombre = c.Nombre,
            Titulo = c.Titulo,
            Descripcion = c.Descripcion,
            TipoGeometria = c.TipoGeometria,
            ColorBorde = c.ColorBorde,
            ColorFondo = c.ColorFondo,
            Opacidad = c.Opacidad,
            GrosorBorde = c.GrosorBorde,
            OrdenVisualizacion = c.OrdenVisualizacion,
            Activo = c.Activo,
            Icono = c.Icono,
            CamposVisibles = c.CamposVisibles
        }).ToList();

        if (resultado.Count == 0)
        {
            resultado = new List<CapaCatalogoDto>
            {
                new()
                {
                    IdCapa = 1,
                    Nombre = "Manzanas",
                    Titulo = "Manzanas Urbanas",
                    Descripcion = "Delimitación física de manzanas del tejido urbano y catastral.",
                    TipoGeometria = "Polygon",
                    ColorBorde = "#d35400",
                    ColorFondo = "#e67e22",
                    Opacidad = 0.35m,
                    GrosorBorde = 2,
                    OrdenVisualizacion = 1,
                    Activo = true,
                    Icono = "bi-grid-3x3",
                    CamposVisibles = "[{\"campo\":\"CodigoManzana\",\"alias\":\"Cód. Manzana\"},{\"campo\":\"Sector\",\"alias\":\"Sector\"},{\"campo\":\"Barrio\",\"alias\":\"Barrio\"},{\"campo\":\"AreaM2\",\"alias\":\"Área (m²)\"},{\"campo\":\"PerimetroM\",\"alias\":\"Perímetro (m)\"}]"
                },
                new()
                {
                    IdCapa = 2,
                    Nombre = "Lotes",
                    Titulo = "Lotes Prediales",
                    Descripcion = "Predios urbanos catastrados con información de uso, titular y área.",
                    TipoGeometria = "Polygon",
                    ColorBorde = "#1f77b4",
                    ColorFondo = "#3498db",
                    Opacidad = 0.55m,
                    GrosorBorde = 2,
                    OrdenVisualizacion = 2,
                    Activo = true,
                    Icono = "bi-houses",
                    CamposVisibles = "[{\"campo\":\"CodigoLote\",\"alias\":\"Cód. Lote\"},{\"campo\":\"CodigoManzana\",\"alias\":\"Manzana\"},{\"campo\":\"NumeroPredial\",\"alias\":\"Nº Predial\"},{\"campo\":\"UsoSuelo\",\"alias\":\"Uso del Suelo\"},{\"campo\":\"Titular\",\"alias\":\"Titular\"},{\"campo\":\"Direccion\",\"alias\":\"Dirección\"},{\"campo\":\"AreaTerrenoM2\",\"alias\":\"Área Terreno (m²)\"}]"
                },
                new()
                {
                    IdCapa = 3,
                    Nombre = "CodigosFijos",
                    Titulo = "Códigos Fijos e Hitos",
                    Descripcion = "Puntos de control geodésico, hitos catastrales y vértices de referencia.",
                    TipoGeometria = "Point",
                    ColorBorde = "#c0392b",
                    ColorFondo = "#e74c3c",
                    Opacidad = 0.95m,
                    GrosorBorde = 3,
                    OrdenVisualizacion = 3,
                    Activo = true,
                    Icono = "bi-geo-alt-fill",
                    CamposVisibles = "[{\"campo\":\"CodigoFijo\",\"alias\":\"Cód. Fijo\"},{\"campo\":\"TipoPunto\",\"alias\":\"Tipo de Hito\"},{\"campo\":\"Descripcion\",\"alias\":\"Descripción\"},{\"campo\":\"Referencia\",\"alias\":\"Referencia\"},{\"campo\":\"CotaZ\",\"alias\":\"Cota Z (m.s.n.m.)\"}]"
                },
                new()
                {
                    IdCapa = 4,
                    Nombre = "Vias",
                    Titulo = "Red Vial y Ejes Viales",
                    Descripcion = "Trazado de avenidas, calles, jirones y pasajes con datos de pavimento y jerarquía.",
                    TipoGeometria = "LineString",
                    ColorBorde = "#2c3e50",
                    ColorFondo = "#34495e",
                    Opacidad = 0.85m,
                    GrosorBorde = 3,
                    OrdenVisualizacion = 4,
                    Activo = true,
                    Icono = "bi-signpost-split",
                    CamposVisibles = "[{\"campo\":\"CodigoVia\",\"alias\":\"Cód. Vía\"},{\"campo\":\"Nombre\",\"alias\":\"Nombre de Vía\"},{\"campo\":\"TipoVia\",\"alias\":\"Tipo de Vía\"},{\"campo\":\"Jerarquia\",\"alias\":\"Jerarquía\"},{\"campo\":\"Sentido\",\"alias\":\"Sentido de Tránsito\"},{\"campo\":\"Pavimento\",\"alias\":\"Pavimento\"},{\"campo\":\"AnchoViaM\",\"alias\":\"Ancho (m)\"},{\"campo\":\"LongitudM\",\"alias\":\"Longitud (m)\"}]"
                }
            };
        }

        _catalogoCache[catKey] = (resultado, DateTime.UtcNow.Add(CacheDuration));
        return resultado;
    }

    public async Task<GeoJsonFeatureCollection> ObtenerGeoJsonCapaAsync(string nombreCapa, BoundingBox? bbox = null, int limite = 5000)
    {
        var key = nombreCapa.Trim().ToLowerInvariant();

        // Si es la solicitud estándar de la capa completa sin bbox restrictivo, servir desde memoria instantáneamente
        if (bbox == null && limite >= 5000)
        {
            if (_geoJsonCache.TryGetValue(key, out var cachedEntry) && cachedEntry.ExpirationUtc > DateTime.UtcNow)
            {
                return cachedEntry.Data;
            }
        }

        var featureCollection = new GeoJsonFeatureCollection();

        switch (nombreCapa.Trim().ToLowerInvariant())
        {
            case "manzanas":
                var manzanas = await _capaRepository.ObtenerManzanasPorBBoxAsync(bbox, limite);
                foreach (var m in manzanas)
                {
                    featureCollection.Features.Add(new GeoJsonFeature
                    {
                        Id = m.IdManzana,
                        Geometry = GeoJsonConverter.WktToGeoJsonGeometry(m.WktGeometria),
                        Properties = new Dictionary<string, object?>
                        {
                            ["id"] = m.IdManzana,
                            ["codigo"] = m.CodigoManzana,
                            ["sector"] = m.Sector,
                            ["barrio"] = m.Barrio,
                            ["areaM2"] = m.AreaM2,
                            ["perimetroM"] = m.PerimetroM,
                            ["origenId"] = m.OrigenId
                        }
                    });
                }
                break;

            case "lotes":
                var lotes = await _capaRepository.ObtenerLotesPorBBoxAsync(bbox, limite);
                foreach (var l in lotes)
                {
                    featureCollection.Features.Add(new GeoJsonFeature
                    {
                        Id = l.IdLote,
                        Geometry = GeoJsonConverter.WktToGeoJsonGeometry(l.WktGeometria),
                        Properties = new Dictionary<string, object?>
                        {
                            ["id"] = l.IdLote,
                            ["codigo"] = l.CodigoLote,
                            ["manzana"] = l.CodigoManzana,
                            ["numeroPredial"] = l.NumeroPredial,
                            ["usoSuelo"] = l.UsoSuelo,
                            ["titular"] = l.Titular,
                            ["direccion"] = l.Direccion,
                            ["areaTerrenoM2"] = l.AreaTerrenoM2,
                            ["frenteM"] = l.FrenteM,
                            ["fondoM"] = l.FondoM,
                            ["origenId"] = l.OrigenId
                        }
                    });
                }
                break;

            case "codigosfijos":
                var codigos = await _capaRepository.ObtenerCodigosFijosPorBBoxAsync(bbox, limite);
                foreach (var c in codigos)
                {
                    featureCollection.Features.Add(new GeoJsonFeature
                    {
                        Id = c.IdCodigoFijo,
                        Geometry = GeoJsonConverter.WktToGeoJsonGeometry(c.WktGeometria),
                        Properties = new Dictionary<string, object?>
                        {
                            ["id"] = c.IdCodigoFijo,
                            ["codigo"] = c.CodigoFijoValor,
                            ["tipoPunto"] = c.TipoPunto,
                            ["descripcion"] = c.Descripcion,
                            ["referencia"] = c.Referencia,
                            ["cotaZ"] = c.CotaZ,
                            ["origenId"] = c.OrigenId
                        }
                    });
                }
                break;

            case "vias":
                var vias = await _capaRepository.ObtenerViasPorBBoxAsync(bbox, limite);
                foreach (var v in vias)
                {
                    featureCollection.Features.Add(new GeoJsonFeature
                    {
                        Id = v.IdVia,
                        Geometry = GeoJsonConverter.WktToGeoJsonGeometry(v.WktGeometria),
                        Properties = new Dictionary<string, object?>
                        {
                            ["id"] = v.IdVia,
                            ["codigo"] = v.CodigoVia,
                            ["nombre"] = v.Nombre,
                            ["tipoVia"] = v.TipoVia,
                            ["jerarquia"] = v.Jerarquia,
                            ["sentido"] = v.Sentido,
                            ["anchoViaM"] = v.AnchoViaM,
                            ["pavimento"] = v.Pavimento,
                            ["longitudM"] = v.LongitudM,
                            ["origenId"] = v.OrigenId
                        }
                    });
                }
                break;

            case "conexionesagua":
            case "agua":
                if (_aguaRepository != null)
                {
                    var conexiones = await _aguaRepository.ObtenerConexionesAguaAsync(null, bbox, null);
                    foreach (var c in conexiones)
                    {
                        featureCollection.Features.Add(new GeoJsonFeature
                        {
                            Id = c.IdConexion,
                            Geometry = GeoJsonConverter.WktToGeoJsonGeometry(c.Wkt) ?? new GeoJsonGeometry
                            {
                                Type = "Point",
                                Coordinates = new double[] { c.Longitud, c.Latitud }
                            },
                            Properties = new Dictionary<string, object?>
                            {
                                ["id"] = c.IdConexion,
                                ["codigo"] = c.CodigoConexion,
                                ["numeroMedidor"] = c.NumeroMedidor,
                                ["codigoLote"] = c.CodigoLote,
                                ["codigoManzana"] = c.CodigoManzana,
                                ["titular"] = c.Titular,
                                ["direccion"] = c.Direccion,
                                ["categoria"] = c.Categoria,
                                ["estadoConexion"] = c.EstadoConexion,
                                ["mesesMora"] = c.MesesMora,
                                ["saldoPendiente"] = c.SaldoPendiente,
                                ["ultimaLectura"] = c.UltimaLectura,
                                ["longitud"] = c.Longitud,
                                ["latitud"] = c.Latitud
                            }
                        });
                    }
                }
                break;

            default:
                throw new ArgumentException($"La capa '{nombreCapa}' no está registrada en el sistema.");
        }

        if (bbox != null)
        {
            featureCollection.Bbox = new double[] { bbox.MinLon, bbox.MinLat, bbox.MaxLon, bbox.MaxLat };
        }
        else if (limite >= 5000)
        {
            _geoJsonCache[key] = (featureCollection, DateTime.UtcNow.Add(CacheDuration));
        }

        return featureCollection;
    }

    public async Task<EntidadDetalleDto?> ObtenerDetalleEntidadAsync(string nombreCapa, int idEntidad)
    {
        if (_aguaRepository != null && (nombreCapa.Equals("ConexionesAgua", StringComparison.OrdinalIgnoreCase) || nombreCapa.Equals("agua", StringComparison.OrdinalIgnoreCase)))
        {
            var con = await _aguaRepository.ObtenerConexionPorIdAsync(idEntidad);
            if (con != null)
            {
                return new EntidadDetalleDto
                {
                    Capa = "ConexionesAgua",
                    IdEntidad = con.IdConexion,
                    Codigo = con.CodigoConexion,
                    Titulo = $"Medidor {con.NumeroMedidor} ({con.EstadoConexion})",
                    Subtitulo = con.Titular,
                    Wkt = con.Wkt,
                    LonCentro = con.Longitud,
                    LatCentro = con.Latitud,
                    Atributos = new Dictionary<string, object?>
                    {
                        ["Titular"] = con.Titular,
                        ["Dirección"] = con.Direccion,
                        ["Estado Servicio"] = con.EstadoConexion,
                        ["Categoría"] = con.Categoria,
                        ["Meses en Mora"] = con.MesesMora,
                        ["Saldo Pendiente"] = con.SaldoPendiente,
                        ["Última Lectura"] = con.UltimaLectura,
                        ["Manzana"] = con.CodigoManzana,
                        ["Lote"] = con.CodigoLote
                    }
                };
            }
        }

        var raw = await _capaRepository.ObtenerEntidadPorIdAsync(nombreCapa, idEntidad);
        if (raw == null) return null;

        // Si es una entidad conocida
        if (raw is Lote l)
        {
            return new EntidadDetalleDto
            {
                Capa = "Lotes",
                IdEntidad = l.IdLote,
                Codigo = l.CodigoLote,
                Titulo = $"Lote {l.CodigoLote}",
                Subtitulo = l.Direccion,
                Wkt = l.WktGeometria,
                LonCentro = l.LonCentro,
                LatCentro = l.LatCentro,
                Atributos = new Dictionary<string, object?>
                {
                    ["Cód. Manzana"] = l.CodigoManzana,
                    ["Nº Predial"] = l.NumeroPredial,
                    ["Uso del Suelo"] = l.UsoSuelo,
                    ["Titular"] = l.Titular,
                    ["Dirección"] = l.Direccion,
                    ["Área Terreno (m²)"] = l.AreaTerrenoM2,
                    ["Frente (m)"] = l.FrenteM,
                    ["Fondo (m)"] = l.FondoM,
                    ["ID Origen SHP"] = l.OrigenId,
                    ["Fecha Registro"] = l.FechaRegistro.ToString("yyyy-MM-dd HH:mm")
                }
            };
        }
        else if (raw is Manzana m)
        {
            return new EntidadDetalleDto
            {
                Capa = "Manzanas",
                IdEntidad = m.IdManzana,
                Codigo = m.CodigoManzana,
                Titulo = $"Manzana {m.CodigoManzana}",
                Subtitulo = $"Sector: {m.Sector} - Barrio: {m.Barrio}",
                Wkt = m.WktGeometria,
                LonCentro = m.LonCentro,
                LatCentro = m.LatCentro,
                Atributos = new Dictionary<string, object?>
                {
                    ["Sector"] = m.Sector,
                    ["Barrio"] = m.Barrio,
                    ["Área (m²)"] = m.AreaM2,
                    ["Perímetro (m)"] = m.PerimetroM,
                    ["ID Origen SHP"] = m.OrigenId,
                    ["Fecha Registro"] = m.FechaRegistro.ToString("yyyy-MM-dd HH:mm")
                }
            };
        }
        else if (raw is CodigoFijo cf)
        {
            return new EntidadDetalleDto
            {
                Capa = "CodigosFijos",
                IdEntidad = cf.IdCodigoFijo,
                Codigo = cf.CodigoFijoValor,
                Titulo = cf.TipoPunto ?? "Código Fijo",
                Subtitulo = cf.Descripcion,
                Wkt = cf.WktGeometria,
                LonCentro = cf.LonCentro,
                LatCentro = cf.LatCentro,
                Atributos = new Dictionary<string, object?>
                {
                    ["Tipo de Hito"] = cf.TipoPunto,
                    ["Descripción"] = cf.Descripcion,
                    ["Referencia"] = cf.Referencia,
                    ["Cota Z (m.s.n.m.)"] = cf.CotaZ,
                    ["ID Origen SHP"] = cf.OrigenId,
                    ["Fecha Registro"] = cf.FechaRegistro.ToString("yyyy-MM-dd HH:mm")
                }
            };
        }
        else if (raw is Via v)
        {
            return new EntidadDetalleDto
            {
                Capa = "Vias",
                IdEntidad = v.IdVia,
                Codigo = v.CodigoVia,
                Titulo = $"{v.TipoVia} {v.Nombre}",
                Subtitulo = $"Jerarquía: {v.Jerarquia}",
                Wkt = v.WktGeometria,
                LonCentro = v.LonCentro,
                LatCentro = v.LatCentro,
                Atributos = new Dictionary<string, object?>
                {
                    ["Nombre"] = v.Nombre,
                    ["Tipo de Vía"] = v.TipoVia,
                    ["Jerarquía"] = v.Jerarquia,
                    ["Sentido"] = v.Sentido,
                    ["Ancho (m)"] = v.AnchoViaM,
                    ["Pavimento"] = v.Pavimento,
                    ["Longitud (m)"] = v.LongitudM,
                    ["ID Origen SHP"] = v.OrigenId,
                    ["Fecha Registro"] = v.FechaRegistro.ToString("yyyy-MM-dd HH:mm")
                }
            };
        }

        return null;
    }
}
