#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Migracion;
using VisorDatosSIG.Application.Exceptions;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.Enums;

namespace VisorDatosSIG.Application.Services;

public class MigradorService : IMigradorService
{
    private readonly IMigracionRepository _migracionRepository;
    private readonly IShapefileReader _shapefileReader;
    private readonly IAuditoriaService _auditoriaService;

    public MigradorService(
        IMigracionRepository migracionRepository, 
        IShapefileReader shapefileReader,
        IAuditoriaService auditoriaService)
    {
        _migracionRepository = migracionRepository;
        _shapefileReader = shapefileReader;
        _auditoriaService = auditoriaService;
    }

    public Task<ValidacionShapefileDto> ValidarShapefileAsync(string rutaShp)
    {
        if (string.IsNullOrWhiteSpace(rutaShp))
            throw new ArgumentException("La ruta del archivo Shapefile no puede ser nula.", nameof(rutaShp));

        var validacion = _shapefileReader.InspeccionarShapefile(rutaShp);
        return Task.FromResult(validacion);
    }

    public Task<List<VistaPreviaRegistroDto>> ObtenerVistaPreviaAsync(string rutaShp, int cantidadFilas = 20)
    {
        if (string.IsNullOrWhiteSpace(rutaShp))
            throw new ArgumentException("La ruta del archivo Shapefile no puede ser nula.", nameof(rutaShp));

        var vistaPrevia = _shapefileReader.LeerVistaPrevia(rutaShp, cantidadFilas);
        return Task.FromResult(vistaPrevia);
    }

    public async Task<ResumenMigracionDto> EjecutarMigracionAsync(
        IniciarMigracionRequestDto request, 
        IProgress<ProgresoMigracionDto>? progreso = null, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var advertencias = new List<string>();
        var errores = new List<string>();

        // 1. Validar precondiciones del shapefile
        var validacion = _shapefileReader.InspeccionarShapefile(request.RutaCarpetaOShapefile);
        if (!validacion.TieneShp || !validacion.TieneShx || !validacion.TieneDbf)
        {
            throw new MigracionException(
                "El Shapefile está incompleto. Se requieren los archivos .shp, .shx y .dbf en la misma carpeta.",
                request.Capa.ToString(), request.RutaCarpetaOShapefile);
        }

        if (!validacion.TienePrj)
        {
            advertencias.Add("El archivo .prj no fue encontrado. Asegúrese de que el shapefile utiliza coordenadas WGS84 (SRID 4326).");
        }
        else if (!validacion.EsWgs84)
        {
            throw new MigracionException(
                "La referencia espacial del archivo .prj no corresponde a WGS 84 (EPSG:4326). Se requiere proyección geográfica WGS 84.",
                request.Capa.ToString(), request.RutaCarpetaOShapefile);
        }

        var nombreArchivo = Path.GetFileName(request.RutaCarpetaOShapefile);
        var totalEsperado = validacion.CantidadRegistros;

        // 2. Registrar inicio de migración en la BD (trazabilidad)
        var idMigracion = await _migracionRepository.IniciarMigracionAsync(
            request.Usuario,
            request.Capa.ToString(),
            nombreArchivo,
            request.RutaCarpetaOShapefile,
            request.Modalidad.ToString(),
            totalEsperado);

        var progresoActual = new ProgresoMigracionDto
        {
            IdMigracion = idMigracion,
            Capa = request.Capa.ToString(),
            TotalRegistros = totalEsperado,
            Procesados = 0,
            Exitosos = 0,
            Omitidos = 0,
            Fallidos = 0,
            MensajeEstado = "Iniciando lectura de registros..."
        };
        progreso?.Report(progresoActual);

        bool reemplazar = request.Modalidad == ModalidadMigracion.Reemplazar;
        int exitosos = 0, omitidos = 0, fallidos = 0;

        try
        {
            // 3. Lectura de registros y mapeo explícito
            var registros = _shapefileReader.LeerRegistrosCompletos(request.RutaCarpetaOShapefile).ToList();
            progresoActual.TotalRegistros = registros.Count;

            switch (request.Capa)
            {
                case TipoCapa.Manzanas:
                    var listaManzanas = new List<Manzana>();
                    foreach (var (origenId, wkt, atributos) in registros)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            if (string.IsNullOrWhiteSpace(wkt))
                            {
                                fallidos++;
                                errores.Add($"Manzana #{origenId} omitida por no contener geometría válida.");
                                continue;
                            }

                            var codigo = ObtenerValorAtributo(atributos, "CodigoManzana", "COD_MZ", "COD_MZA", "COD_MANZANA", "MZ", "MZA", "CODIGO") ?? $"MZ-{origenId}";
                            var sector = ObtenerValorAtributo(atributos, "Sector", "SECTOR", "SEC");
                            var barrio = ObtenerValorAtributo(atributos, "Barrio", "BARRIO", "URB", "URBANIZACION");
                            var area = ObtenerDecimalAtributo(atributos, "AreaM2", "AREA", "AREA_M2", "SHAPE_AREA");
                            var perimetro = ObtenerDecimalAtributo(atributos, "PerimetroM", "PERIMETRO", "PERIM", "SHAPE_LEN");

                            listaManzanas.Add(new Manzana
                            {
                                CodigoManzana = codigo,
                                Sector = sector,
                                Barrio = barrio,
                                AreaM2 = area,
                                PerimetroM = perimetro,
                                OrigenId = origenId,
                                WktGeometria = wkt,
                                IdMigracion = idMigracion
                            });
                        }
                        catch (Exception ex)
                        {
                            fallidos++;
                            errores.Add($"Error al mapear registro #{origenId}: {ex.Message}");
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        progreso?.Report(new ProgresoMigracionDto
                        {
                            IdMigracion = idMigracion,
                            Capa = request.Capa.ToString(),
                            TotalRegistros = listaManzanas.Count,
                            Procesados = 0,
                            MensajeEstado = "Insertando registros en SQL Server..."
                        });

                        exitosos = await _migracionRepository.InsertarManzanasPorLoteAsync(
                            listaManzanas, idMigracion, reemplazar, cancellationToken, (proc, tot) =>
                            {
                                progreso?.Report(new ProgresoMigracionDto
                                {
                                    IdMigracion = idMigracion,
                                    Capa = request.Capa.ToString(),
                                    TotalRegistros = tot,
                                    Procesados = proc,
                                    Exitosos = proc,
                                    Fallidos = fallidos,
                                    MensajeEstado = $"Insertando en SQL Server: {proc}/{tot}..."
                                });
                            });
                        omitidos = listaManzanas.Count - exitosos;
                    }
                    break;

                case TipoCapa.Lotes:
                    var listaLotes = new List<Lote>();
                    foreach (var (origenId, wkt, atributos) in registros)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            if (string.IsNullOrWhiteSpace(wkt))
                            {
                                fallidos++;
                                errores.Add($"Lote #{origenId} omitido por no contener geometría válida.");
                                continue;
                            }

                            var codigo = ObtenerValorAtributo(atributos, "CodigoLote", "COD_LOTE", "LOTE", "CODIGO", "ID_LOTE") ?? $"LT-{origenId}";
                            var codMz = ObtenerValorAtributo(atributos, "CodigoManzana", "COD_MZ", "COD_MZA", "MANZANA", "MZ", "MZA");
                            var numPredial = ObtenerValorAtributo(atributos, "NumeroPredial", "NUM_PRED", "PREDIO", "NUMERO");
                            var usoSuelo = ObtenerValorAtributo(atributos, "UsoSuelo", "USO_SUELO", "USO", "ZONIFICACION");
                            var titular = ObtenerValorAtributo(atributos, "Titular", "TITULAR", "PROPIETARIO", "NOMBRE");
                            var direccion = ObtenerValorAtributo(atributos, "Direccion", "DIRECCION", "DIR", "UBICACION");
                            var area = ObtenerDecimalAtributo(atributos, "AreaTerrenoM2", "AREA_TERRENO", "AREA", "SHAPE_AREA");
                            var frente = ObtenerDecimalAtributo(atributos, "FrenteM", "FRENTE", "FRENTE_M");
                            var fondo = ObtenerDecimalAtributo(atributos, "FondoM", "FONDO", "FONDO_M");

                            listaLotes.Add(new Lote
                            {
                                CodigoLote = codigo,
                                CodigoManzana = codMz,
                                NumeroPredial = numPredial,
                                UsoSuelo = usoSuelo,
                                Titular = titular,
                                Direccion = direccion,
                                AreaTerrenoM2 = area,
                                FrenteM = frente,
                                FondoM = fondo,
                                OrigenId = origenId,
                                WktGeometria = wkt,
                                IdMigracion = idMigracion
                            });
                        }
                        catch (Exception ex)
                        {
                            fallidos++;
                            errores.Add($"Error al mapear Lote #{origenId}: {ex.Message}");
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        progreso?.Report(new ProgresoMigracionDto
                        {
                            IdMigracion = idMigracion,
                            Capa = request.Capa.ToString(),
                            TotalRegistros = listaLotes.Count,
                            Procesados = 0,
                            MensajeEstado = "Insertando registros en SQL Server..."
                        });

                        exitosos = await _migracionRepository.InsertarLotesPorLoteAsync(
                            listaLotes, idMigracion, reemplazar, cancellationToken, (proc, tot) =>
                            {
                                progreso?.Report(new ProgresoMigracionDto
                                {
                                    IdMigracion = idMigracion,
                                    Capa = request.Capa.ToString(),
                                    TotalRegistros = tot,
                                    Procesados = proc,
                                    Exitosos = proc,
                                    Fallidos = fallidos,
                                    MensajeEstado = $"Insertando en SQL Server: {proc}/{tot}..."
                                });
                            });
                        omitidos = listaLotes.Count - exitosos;
                    }
                    break;

                case TipoCapa.CodigosFijos:
                    var listaCodigos = new List<CodigoFijo>();
                    foreach (var (origenId, wkt, atributos) in registros)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            if (string.IsNullOrWhiteSpace(wkt))
                            {
                                fallidos++;
                                errores.Add($"Código Fijo #{origenId} omitido por no contener geometría válida.");
                                continue;
                            }

                            var codigo = ObtenerValorAtributo(atributos, "CodigoFijo", "COD_FIJO", "CODFIJO", "PUNTO", "CODIGO") ?? $"CF-{origenId}";
                            var tipo = ObtenerValorAtributo(atributos, "TipoPunto", "TIPO_PUNTO", "TIPO", "CLASE");
                            var desc = ObtenerValorAtributo(atributos, "Descripcion", "DESCRIPCION", "DESC", "NOMBRE");
                            var refUbicacion = ObtenerValorAtributo(atributos, "Referencia", "REFERENCIA", "REF");
                            var cota = ObtenerDecimalAtributo(atributos, "CotaZ", "COTA_Z", "COTA", "ELEVACION", "Z");

                            listaCodigos.Add(new CodigoFijo
                            {
                                CodigoFijoValor = codigo,
                                TipoPunto = tipo,
                                Descripcion = desc,
                                Referencia = refUbicacion,
                                CotaZ = cota,
                                OrigenId = origenId,
                                WktGeometria = wkt,
                                IdMigracion = idMigracion
                            });
                        }
                        catch (Exception ex)
                        {
                            fallidos++;
                            errores.Add($"Error al mapear Código Fijo #{origenId}: {ex.Message}");
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        progreso?.Report(new ProgresoMigracionDto
                        {
                            IdMigracion = idMigracion,
                            Capa = request.Capa.ToString(),
                            TotalRegistros = listaCodigos.Count,
                            Procesados = 0,
                            MensajeEstado = "Insertando registros en SQL Server..."
                        });

                        exitosos = await _migracionRepository.InsertarCodigosFijosPorLoteAsync(
                            listaCodigos, idMigracion, reemplazar, cancellationToken, (proc, tot) =>
                            {
                                progreso?.Report(new ProgresoMigracionDto
                                {
                                    IdMigracion = idMigracion,
                                    Capa = request.Capa.ToString(),
                                    TotalRegistros = tot,
                                    Procesados = proc,
                                    Exitosos = proc,
                                    Fallidos = fallidos,
                                    MensajeEstado = $"Insertando en SQL Server: {proc}/{tot}..."
                                });
                            });
                        omitidos = listaCodigos.Count - exitosos;
                    }
                    break;

                case TipoCapa.Vias:
                    var listaVias = new List<Via>();
                    foreach (var (origenId, wkt, atributos) in registros)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            if (string.IsNullOrWhiteSpace(wkt))
                            {
                                fallidos++;
                                errores.Add($"Vía #{origenId} omitida por no contener geometría válida.");
                                continue;
                            }

                            var codigo = ObtenerValorAtributo(atributos, "CodigoVia", "COD_VIA", "CODVIA", "VIA", "CODIGO") ?? $"VIA-{origenId}";
                            var nombre = ObtenerValorAtributo(atributos, "Nombre", "NOMBRE_VIA", "NOM_VIA", "NOMVIA", "NOMBRE", "CALLE") ?? $"Vía {codigo}";
                            var tipo = ObtenerValorAtributo(atributos, "TipoVia", "TIPO_VIA", "TIPOVIA", "TIPO", "CLASE_VIA");
                            var jerarquia = ObtenerValorAtributo(atributos, "Jerarquia", "JERARQUIA", "NIVEL_VIA", "JERARQUICA");
                            var sentido = ObtenerValorAtributo(atributos, "Sentido", "SENTIDO", "SENTIDO_VI");
                            var ancho = ObtenerDecimalAtributo(atributos, "AnchoViaM", "ANCHO_VIA", "ANCHO", "SECCION");
                            var pavimento = ObtenerValorAtributo(atributos, "Pavimento", "PAVIMENTO", "TIPO_PAV", "SUPERFICIE");
                            var longitud = ObtenerDecimalAtributo(atributos, "LongitudM", "LONGITUD", "LONG", "SHAPE_LEN");

                            listaVias.Add(new Via
                            {
                                CodigoVia = codigo,
                                Nombre = nombre,
                                TipoVia = tipo,
                                Jerarquia = jerarquia,
                                Sentido = sentido,
                                AnchoViaM = ancho,
                                Pavimento = pavimento,
                                LongitudM = longitud,
                                OrigenId = origenId,
                                WktGeometria = wkt,
                                IdMigracion = idMigracion
                            });
                        }
                        catch (Exception ex)
                        {
                            fallidos++;
                            errores.Add($"Error al mapear Vía #{origenId}: {ex.Message}");
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        progreso?.Report(new ProgresoMigracionDto
                        {
                            IdMigracion = idMigracion,
                            Capa = request.Capa.ToString(),
                            TotalRegistros = listaVias.Count,
                            Procesados = 0,
                            MensajeEstado = "Insertando registros en SQL Server..."
                        });

                        exitosos = await _migracionRepository.InsertarViasPorLoteAsync(
                            listaVias, idMigracion, reemplazar, cancellationToken, (proc, tot) =>
                            {
                                progreso?.Report(new ProgresoMigracionDto
                                {
                                    IdMigracion = idMigracion,
                                    Capa = request.Capa.ToString(),
                                    TotalRegistros = tot,
                                    Procesados = proc,
                                    Exitosos = proc,
                                    Fallidos = fallidos,
                                    MensajeEstado = $"Insertando en SQL Server: {proc}/{tot}..."
                                });
                            });
                        omitidos = listaVias.Count - exitosos;
                    }
                    break;
            }

            stopwatch.Stop();

            string estadoFinal;
            if (cancellationToken.IsCancellationRequested)
            {
                estadoFinal = "Cancelado";
                advertencias.Add("La operación de migración fue cancelada por el usuario.");
            }
            else if (errores.Count > 0 && exitosos == 0)
            {
                estadoFinal = "Fallido";
            }
            else
            {
                estadoFinal = "Exitoso";
                CapaService.InvalidarCacheStatic(request.Capa);
            }

            var detalleJson = FormatearDetalleJson(advertencias, errores, stopwatch.ElapsedMilliseconds);

            await _migracionRepository.ActualizarEstadoMigracionAsync(
                idMigracion, exitosos, omitidos, fallidos, estadoFinal, detalleJson);

            await _auditoriaService.RegistrarEventoAsync(
                "Migracion", "Migrador",
                $"Migración de capa '{request.Capa}' ({estadoFinal}). Exitosos: {exitosos}, Omitidos: {omitidos}, Fallidos: {fallidos}.",
                request.Usuario, detalleTecnico: detalleJson, exitoso: estadoFinal == "Exitoso");

            progresoActual.Procesados = exitosos + omitidos + fallidos;
            progresoActual.Exitosos = exitosos;
            progresoActual.Omitidos = omitidos;
            progresoActual.Fallidos = fallidos;
            progresoActual.HaTerminado = true;
            progresoActual.EsCancelado = cancellationToken.IsCancellationRequested;
            progresoActual.MensajeEstado = $"Migración finalizada con estado: {estadoFinal}.";
            progreso?.Report(progresoActual);

            return new ResumenMigracionDto
            {
                IdMigracion = idMigracion,
                FechaInicio = DateTime.UtcNow.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                FechaFin = DateTime.UtcNow,
                Usuario = request.Usuario,
                Capa = request.Capa.ToString(),
                NombreArchivo = nombreArchivo,
                Modalidad = request.Modalidad.ToString(),
                TotalRegistros = totalEsperado,
                RegistrosExitosos = exitosos,
                RegistrosOmitidos = omitidos,
                RegistrosFallidos = fallidos,
                Estado = estadoFinal,
                DuracionMs = stopwatch.ElapsedMilliseconds,
                Advertencias = advertencias,
                Errores = errores
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var detalleErr = $"{{\"Error\":\"{EscapeJson(ex.Message)}\"}}";
            await _migracionRepository.ActualizarEstadoMigracionAsync(
                idMigracion, exitosos, omitidos, fallidos + 1, "Fallido", detalleErr);

            await _auditoriaService.RegistrarEventoAsync(
                "Migracion", "Migrador",
                $"Error crítico durante migración de '{request.Capa}': {ex.Message}",
                request.Usuario, detalleTecnico: detalleErr, exitoso: false);

            throw;
        }
    }

    public async Task<List<ResumenMigracionDto>> ObtenerHistorialMigracionesAsync()
    {
        var lista = await _migracionRepository.ObtenerHistorialMigracionesAsync();
        return lista.Select(m => new ResumenMigracionDto
        {
            IdMigracion = m.IdMigracion,
            FechaInicio = m.FechaInicio,
            FechaFin = m.FechaFin,
            Usuario = m.Usuario,
            Capa = m.Capa,
            NombreArchivo = m.NombreArchivo,
            Modalidad = m.Modalidad,
            TotalRegistros = m.TotalRegistros,
            RegistrosExitosos = m.RegistrosExitosos,
            RegistrosOmitidos = m.RegistrosOmitidos,
            RegistrosFallidos = m.RegistrosFallidos,
            Estado = m.Estado,
            DuracionMs = m.DuracionMs
        }).ToList();
    }

    #region Helpers de extracción y formateo
    private static string? ObtenerValorAtributo(Dictionary<string, object?> atributos, params string[] nombresPosibles)
    {
        foreach (var nom in nombresPosibles)
        {
            var entry = atributos.FirstOrDefault(kv => kv.Key.Equals(nom, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(entry.Key) && entry.Value != null)
            {
                var val = entry.Value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val)) return val;
            }
        }
        return null;
    }

    private static decimal? ObtenerDecimalAtributo(Dictionary<string, object?> atributos, params string[] nombresPosibles)
    {
        var str = ObtenerValorAtributo(atributos, nombresPosibles);
        if (string.IsNullOrWhiteSpace(str)) return null;

        if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var decVal))
            return decVal;

        if (decimal.TryParse(str, out var decLocal))
            return decLocal;

        return null;
    }

    private static string FormatearDetalleJson(List<string> advertencias, List<string> errores, long duracionMs)
    {
        var advList = string.Join(",", advertencias.Where(a => a != null).Select(a => $"\"{EscapeJson(a)}\""));
        var errList = string.Join(",", errores.Where(e => e != null).Select(e => $"\"{EscapeJson(e)}\""));
        return $"{{\"Advertencias\":[{advList}],\"Errores\":[{errList}],\"DuracionMs\":{duracionMs}}}";
    }

    private static string EscapeJson(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s!.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
    }
    #endregion
}
