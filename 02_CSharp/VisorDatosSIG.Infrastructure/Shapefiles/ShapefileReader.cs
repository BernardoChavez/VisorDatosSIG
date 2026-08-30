#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VisorDatosSIG.Application.DTOs.Migracion;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.Enums;

namespace VisorDatosSIG.Infrastructure.Shapefiles;

public class ShapefileReader : IShapefileReader
{
    static ShapefileReader()
    {
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
    }

    public ValidacionShapefileDto InspeccionarShapefile(string rutaShp)
    {
        var dto = new ValidacionShapefileDto
        {
            RutaShp = rutaShp,
            NombreCapaDetectada = Path.GetFileNameWithoutExtension(rutaShp)
        };

        if (string.IsNullOrWhiteSpace(rutaShp))
        {
            dto.Errores.Add("La ruta especificada es inválida.");
            return dto;
        }

        var dir = Path.GetDirectoryName(rutaShp) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(rutaShp);

        var shpPath = Path.Combine(dir, baseName + ".shp");
        var shxPath = Path.Combine(dir, baseName + ".shx");
        var dbfPath = Path.Combine(dir, baseName + ".dbf");
        var prjPath = Path.Combine(dir, baseName + ".prj");

        dto.TieneShp = File.Exists(shpPath);
        dto.TieneShx = File.Exists(shxPath);
        dto.TieneDbf = File.Exists(dbfPath);
        dto.TienePrj = File.Exists(prjPath);

        if (!dto.TieneShp) dto.Errores.Add("Falta el archivo principal de geometrías (.shp).");
        if (!dto.TieneShx) dto.Errores.Add("Falta el archivo de índice posicional (.shx).");
        if (!dto.TieneDbf) dto.Errores.Add("Falta la tabla de atributos alfanuméricos (.dbf).");
        if (!dto.TienePrj) dto.Advertencias.Add("Falta el archivo de proyección espacial (.prj).");

        // Detección de Capa por nombre
        var lowerName = baseName.ToLowerInvariant();
        if (lowerName.Contains("manzana") || lowerName.Contains("mza") || lowerName.Contains("mzn") || lowerName.Contains("_mz")) dto.CapaEnum = TipoCapa.Manzanas;
        else if (lowerName.Contains("lote")) dto.CapaEnum = TipoCapa.Lotes;
        else if (lowerName.Contains("fijo") || lowerName.Contains("codigo") || lowerName.Contains("punto") || lowerName.Contains("hito")) dto.CapaEnum = TipoCapa.CodigosFijos;
        else if (lowerName.Contains("via") || lowerName.Contains("calle") || lowerName.Contains("eje")) dto.CapaEnum = TipoCapa.Vias;

        // Validar .prj si existe
        if (dto.TienePrj)
        {
            try
            {
                var prjText = File.ReadAllText(prjPath);
                dto.ContenidoPrj = prjText;
                dto.EsWgs84 = prjText.IndexOf("WGS_1984", StringComparison.OrdinalIgnoreCase) >= 0
                           || prjText.IndexOf("WGS 84", StringComparison.OrdinalIgnoreCase) >= 0
                           || prjText.IndexOf("4326", StringComparison.OrdinalIgnoreCase) >= 0
                           || prjText.IndexOf("GCS_WGS_1984", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!dto.EsWgs84)
                {
                    dto.Errores.Add("El archivo .prj no corresponde a la proyección geográfica WGS 84 (EPSG:4326).");
                }
            }
            catch (Exception ex)
            {
                dto.Errores.Add($"Error al leer archivo .prj: {ex.Message}");
            }
        }

        // Inspeccionar .shp header
        if (dto.TieneShp)
        {
            try
            {
                using var fs = new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryReader(fs);

                if (fs.Length >= 100)
                {
                    int fileCode = LeerEnteroBigEndian(br);
                    if (fileCode != 9994)
                    {
                        dto.Errores.Add("El archivo no posee una cabecera ESRI Shapefile válida (FileCode != 9994).");
                    }

                    fs.Seek(32, SeekOrigin.Begin);
                    int shapeType = br.ReadInt32();
                    dto.TipoGeometria = ObtenerNombreShapeType(shapeType);
                }
            }
            catch (Exception ex)
            {
                dto.Errores.Add($"Error al inspeccionar archivo .shp: {ex.Message}");
            }
        }

        // Inspeccionar .dbf header y campos
        if (dto.TieneDbf)
        {
            try
            {
                var info = LeerInfoDbf(dbfPath);
                dto.CantidadRegistros = info.CantidadRegistros;
                dto.CamposDbf = info.Campos.Select(c => c.Nombre).ToList();
            }
            catch (Exception ex)
            {
                dto.Errores.Add($"Error al inspeccionar archivo .dbf: {ex.Message}");
            }
        }

        return dto;
    }

    public List<VistaPreviaRegistroDto> LeerVistaPrevia(string rutaShp, int cantidad = 20)
    {
        var lista = new List<VistaPreviaRegistroDto>();
        var registros = LeerRegistrosCompletos(rutaShp).Take(cantidad);

        int index = 1;
        foreach (var (origenId, wkt, atributos) in registros)
        {
            lista.Add(new VistaPreviaRegistroDto
            {
                IndiceFila = index++,
                OrigenId = origenId,
                Atributos = atributos,
                WktResumen = wkt.Length > 80 ? wkt.Substring(0, 77) + "..." : wkt,
                GeometriaValida = !string.IsNullOrWhiteSpace(wkt)
            });
        }

        return lista;
    }

    public IEnumerable<(string OrigenId, string Wkt, Dictionary<string, object?> Atributos)> LeerRegistrosCompletos(string rutaShp)
    {
        var dir = Path.GetDirectoryName(rutaShp) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(rutaShp);
        var shpPath = Path.Combine(dir, baseName + ".shp");
        var dbfPath = Path.Combine(dir, baseName + ".dbf");

        if (!File.Exists(shpPath) || !File.Exists(dbfPath))
            yield break;

        var dbfInfo = LeerInfoDbf(dbfPath);

        using var fsShp = new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var brShp = new BinaryReader(fsShp);

        using var fsDbf = new FileStream(dbfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var brDbf = new BinaryReader(fsDbf);

        // Posicionar en primer registro de DBF
        fsDbf.Seek(dbfInfo.LongitudCabecera, SeekOrigin.Begin);

        // Posicionar en primer registro de SHP
        fsShp.Seek(100, SeekOrigin.Begin);

        int recordIndex = 1;
        while (fsShp.Position < fsShp.Length && recordIndex <= dbfInfo.CantidadRegistros)
        {
            // Leer registro SHP
            int recNum = LeerEnteroBigEndian(brShp);
            int contentLengthWords = LeerEnteroBigEndian(brShp);
            long nextRecordPos = fsShp.Position + (contentLengthWords * 2);

            int shapeType = brShp.ReadInt32();
            string wkt = "";

            if (shapeType == 1 || shapeType == 11 || shapeType == 21) // Point, PointZ, PointM
            {
                double x = brShp.ReadDouble();
                double y = brShp.ReadDouble();
                wkt = string.Format(CultureInfo.InvariantCulture, "POINT({0} {1})", x, y);
            }
            else if (shapeType == 3 || shapeType == 13 || shapeType == 23 || shapeType == 5 || shapeType == 15 || shapeType == 25) // PolyLine or Polygon (2D, Z, M)
            {
                // Box (4 doubles = 32 bytes)
                double boxMinX = brShp.ReadDouble();
                double boxMinY = brShp.ReadDouble();
                double boxMaxX = brShp.ReadDouble();
                double boxMaxY = brShp.ReadDouble();

                int numParts = brShp.ReadInt32();
                int numPoints = brShp.ReadInt32();

                if (numParts > 0 && numPoints > 0)
                {
                    int[] parts = new int[numParts];
                    for (int p = 0; p < numParts; p++)
                    {
                        parts[p] = brShp.ReadInt32();
                    }

                    var points = new (double X, double Y)[numPoints];
                    for (int pt = 0; pt < numPoints; pt++)
                    {
                        points[pt] = (brShp.ReadDouble(), brShp.ReadDouble());
                    }

                    if (shapeType == 3 || shapeType == 13 || shapeType == 23) // PolyLine -> LINESTRING / MULTILINESTRING
                    {
                        if (numParts == 1)
                        {
                            var ptsStr = string.Join(", ", points.Select(p => string.Format(CultureInfo.InvariantCulture, "{0} {1}", p.X, p.Y)));
                            wkt = $"LINESTRING({ptsStr})";
                        }
                        else
                        {
                            var lineParts = new List<string>();
                            for (int p = 0; p < numParts; p++)
                            {
                                int start = parts[p];
                                int end = (p == numParts - 1) ? numPoints : parts[p + 1];
                                var subPts = points.Skip(start).Take(end - start);
                                lineParts.Add("(" + string.Join(", ", subPts.Select(pt => string.Format(CultureInfo.InvariantCulture, "{0} {1}", pt.X, pt.Y))) + ")");
                            }
                            wkt = $"MULTILINESTRING({string.Join(", ", lineParts)})";
                        }
                    }
                    else // Polygon -> POLYGON
                    {
                        var ringParts = new List<string>();
                        for (int p = 0; p < numParts; p++)
                        {
                            int start = parts[p];
                            int end = (p == numParts - 1) ? numPoints : parts[p + 1];
                            var subPts = points.Skip(start).Take(end - start).ToList();

                            // Asegurar que el polígono esté cerrado con al menos 3 vértices
                            if (subPts.Count >= 3)
                            {
                                if (subPts[0].X != subPts[subPts.Count - 1].X || subPts[0].Y != subPts[subPts.Count - 1].Y)
                                {
                                    subPts.Add(subPts[0]);
                                }

                                ringParts.Add("(" + string.Join(", ", subPts.Select(pt => string.Format(CultureInfo.InvariantCulture, "{0} {1}", pt.X, pt.Y))) + ")");
                            }
                        }

                        if (ringParts.Count > 0)
                        {
                            wkt = $"POLYGON({string.Join(", ", ringParts)})";
                        }
                    }
                }
            }

            if (nextRecordPos >= 0 && nextRecordPos <= fsShp.Length)
            {
                fsShp.Seek(nextRecordPos, SeekOrigin.Begin);
            }
            else
            {
                break;
            }

            // Leer registro DBF
            byte deletedFlag = brDbf.ReadByte();
            var atributos = new Dictionary<string, object?>();

            foreach (var campo in dbfInfo.Campos)
            {
                byte[] rawVal = brDbf.ReadBytes(campo.Longitud);
                string strVal = Encoding.GetEncoding(1252).GetString(rawVal).Trim();

                if (string.IsNullOrEmpty(strVal))
                {
                    atributos[campo.Nombre] = null;
                }
                else
                {
                    atributos[campo.Nombre] = strVal;
                }
            }

            string origenId = recordIndex.ToString();
            yield return (origenId, wkt, atributos);

            recordIndex++;
        }
    }

    #region Métodos privados de bajo nivel
    private static int LeerEnteroBigEndian(BinaryReader br)
    {
        byte[] bytes = br.ReadBytes(4);
        if (bytes.Length < 4) return 0;
        Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static string ObtenerNombreShapeType(int shapeType)
    {
        return shapeType switch
        {
            1 => "Point",
            3 => "PolyLine",
            5 => "Polygon",
            8 => "MultiPoint",
            11 => "PointZ",
            13 => "PolyLineZ",
            15 => "PolygonZ",
            _ => $"Tipo Desconocido ({shapeType})"
        };
    }

    private class CampoDbfInfo
    {
        public string Nombre { get; set; } = string.Empty;
        public char Tipo { get; set; }
        public byte Longitud { get; set; }
        public byte Decimales { get; set; }
    }

    private class DbfHeaderInfo
    {
        public int CantidadRegistros { get; set; }
        public short LongitudCabecera { get; set; }
        public short LongitudRegistro { get; set; }
        public List<CampoDbfInfo> Campos { get; set; } = new();
    }

    private static DbfHeaderInfo LeerInfoDbf(string dbfPath)
    {
        var info = new DbfHeaderInfo();
        using var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        byte version = br.ReadByte();
        byte year = br.ReadByte();
        byte month = br.ReadByte();
        byte day = br.ReadByte();

        info.CantidadRegistros = br.ReadInt32();
        info.LongitudCabecera = br.ReadInt16();
        info.LongitudRegistro = br.ReadInt16();

        fs.Seek(32, SeekOrigin.Begin);

        while (fs.Position < info.LongitudCabecera)
        {
            byte firstByte = br.ReadByte();
            if (firstByte == 0x0D) break; // Fin de descriptores

            byte[] nameBytes = new byte[11];
            nameBytes[0] = firstByte;
            byte[] restName = br.ReadBytes(10);
            Array.Copy(restName, 0, nameBytes, 1, Math.Min(restName.Length, 10));

            string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').Trim();
            char tipo = (char)br.ReadByte();
            br.ReadInt32(); // Offset reservado
            byte longitud = br.ReadByte();
            byte decimales = br.ReadByte();
            br.ReadBytes(14); // Reservado

            info.Campos.Add(new CampoDbfInfo
            {
                Nombre = name,
                Tipo = tipo,
                Longitud = longitud,
                Decimales = decimales
            });
        }

        return info;
    }
    #endregion
}
