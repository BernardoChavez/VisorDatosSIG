#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using VisorDatosSIG.Application.DTOs.GeoJson;

namespace VisorDatosSIG.Application.Services;

/// <summary>
/// Convertidor de alta precisión y ultra alto rendimiento para transformar geometrías WKT (2D/3D) a estructuras RFC 7946 GeoJSON.
/// </summary>
public static class GeoJsonConverter
{
    private static readonly Regex PointRegex = new(@"POINT\s*(?:Z|M|ZM)?\s*\(\s*([-\d\.\+eE]+)\s+([-\d\.\+eE]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LineStringRegex = new(@"LINESTRING\s*(?:Z|M|ZM)?\s*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex PolygonRegex = new(@"POLYGON\s*(?:Z|M|ZM)?\s*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex MultiPointRegex = new(@"MULTIPOINT\s*(?:Z|M|ZM)?\s*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex MultiLineStringRegex = new(@"MULTILINESTRING\s*(?:Z|M|ZM)?\s*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex MultiPolygonRegex = new(@"MULTIPOLYGON\s*(?:Z|M|ZM)?\s*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex RingRegex = new(@"\(([^()]+)\)", RegexOptions.Compiled);
    private static readonly Regex MultiPolygonGroupRegex = new(@"\(\s*\(([^()]+(?:\s*,\s*\([^()]+\))*)\)\s*\)", RegexOptions.Compiled);

    public static GeoJsonGeometry? WktToGeoJsonGeometry(string? wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
            return null;

        var trimmed = wkt!.Trim();

        // 1. Evaluar MULTI primero para evitar colisiones de prefijo
        if (trimmed.StartsWith("MULTIPOLYGON", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMultiPolygon(trimmed);
        }
        else if (trimmed.StartsWith("MULTILINESTRING", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMultiLineString(trimmed);
        }
        else if (trimmed.StartsWith("MULTIPOINT", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMultiPoint(trimmed);
        }
        else if (trimmed.StartsWith("POINT", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePoint(trimmed);
        }
        else if (trimmed.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePolygon(trimmed);
        }
        else if (trimmed.StartsWith("LINESTRING", StringComparison.OrdinalIgnoreCase))
        {
            return ParseLineString(trimmed);
        }

        return null;
    }

    private static GeoJsonGeometry ParsePoint(string wkt)
    {
        int openParen = wkt.IndexOf('(');
        int closeParen = wkt.LastIndexOf(')');
        if (openParen >= 0 && closeParen > openParen)
        {
            var content = wkt.Substring(openParen + 1, closeParen - openParen - 1);
            var coords = ParseCoordinateList(content);
            if (coords.Count > 0)
            {
                return new GeoJsonGeometry
                {
                    Type = "Point",
                    Coordinates = coords[0]
                };
            }
        }

        var match = PointRegex.Match(wkt);
        if (!match.Success)
            throw new FormatException($"Formato WKT POINT inválido: {wkt}");

        var lon = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var lat = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        return new GeoJsonGeometry
        {
            Type = "Point",
            Coordinates = new double[] { lon, lat }
        };
    }

    private static GeoJsonGeometry ParseLineString(string wkt)
    {
        int openParen = wkt.IndexOf('(');
        int closeParen = wkt.LastIndexOf(')');
        if (openParen >= 0 && closeParen > openParen)
        {
            var content = wkt.Substring(openParen + 1, closeParen - openParen - 1);
            var coords = ParseCoordinateList(content);
            return new GeoJsonGeometry
            {
                Type = "LineString",
                Coordinates = coords
            };
        }

        var match = LineStringRegex.Match(wkt);
        if (!match.Success)
            throw new FormatException($"Formato WKT LINESTRING inválido: {wkt}");

        return new GeoJsonGeometry
        {
            Type = "LineString",
            Coordinates = ParseCoordinateList(match.Groups[1].Value)
        };
    }

    private static GeoJsonGeometry ParsePolygon(string wkt)
    {
        var rings = new List<List<double[]>>();
        var ringsMatches = RingRegex.Matches(wkt);

        foreach (Match ringMatch in ringsMatches)
        {
            var coords = ParseCoordinateList(ringMatch.Groups[1].Value);
            if (coords.Count > 0)
            {
                rings.Add(coords);
            }
        }

        if (rings.Count == 0)
        {
            // Polígono simple sin paréntesis anidados
            int openParen = wkt.IndexOf('(');
            int closeParen = wkt.LastIndexOf(')');
            if (openParen >= 0 && closeParen > openParen)
            {
                var content = wkt.Substring(openParen + 1, closeParen - openParen - 1).Trim('(', ')');
                var coords = ParseCoordinateList(content);
                if (coords.Count > 0)
                {
                    rings.Add(coords);
                }
            }
        }

        return new GeoJsonGeometry
        {
            Type = "Polygon",
            Coordinates = rings
        };
    }

    private static GeoJsonGeometry ParseMultiPoint(string wkt)
    {
        int openParen = wkt.IndexOf('(');
        int closeParen = wkt.LastIndexOf(')');
        if (openParen >= 0 && closeParen > openParen)
        {
            var clean = wkt.Substring(openParen + 1, closeParen - openParen - 1).Replace("(", "").Replace(")", "");
            return new GeoJsonGeometry
            {
                Type = "MultiPoint",
                Coordinates = ParseCoordinateList(clean)
            };
        }

        var match = MultiPointRegex.Match(wkt);
        if (!match.Success)
            throw new FormatException($"Formato WKT MULTIPOINT inválido: {wkt}");

        var cleanFallback = match.Groups[1].Value.Replace("(", "").Replace(")", "");
        return new GeoJsonGeometry
        {
            Type = "MultiPoint",
            Coordinates = ParseCoordinateList(cleanFallback)
        };
    }

    private static GeoJsonGeometry ParseMultiLineString(string wkt)
    {
        var lines = new List<List<double[]>>();
        var lineMatches = RingRegex.Matches(wkt);

        foreach (Match lineMatch in lineMatches)
        {
            var coords = ParseCoordinateList(lineMatch.Groups[1].Value);
            if (coords.Count > 0)
            {
                lines.Add(coords);
            }
        }

        return new GeoJsonGeometry
        {
            Type = "MultiLineString",
            Coordinates = lines
        };
    }

    private static GeoJsonGeometry ParseMultiPolygon(string wkt)
    {
        var polygons = new List<List<List<double[]>>>();
        var polyMatches = MultiPolygonGroupRegex.Matches(wkt);

        foreach (Match polyMatch in polyMatches)
        {
            var polyContent = polyMatch.Groups[1].Value;
            var rings = new List<List<double[]>>();
            var ringMatches = RingRegex.Matches(polyContent);
            foreach (Match rm in ringMatches)
            {
                var coords = ParseCoordinateList(rm.Groups[1].Value);
                if (coords.Count > 0)
                {
                    rings.Add(coords);
                }
            }
            if (rings.Count == 0)
            {
                var coords = ParseCoordinateList(polyContent.Trim('(', ')'));
                if (coords.Count > 0)
                {
                    rings.Add(coords);
                }
            }
            if (rings.Count > 0)
            {
                polygons.Add(rings);
            }
        }

        if (polygons.Count == 0)
        {
            // Si viene como una sola lista de anillos
            var rings = new List<List<double[]>>();
            var singleRings = RingRegex.Matches(wkt);
            foreach (Match r in singleRings)
            {
                var coords = ParseCoordinateList(r.Groups[1].Value);
                if (coords.Count > 0)
                {
                    rings.Add(coords);
                }
            }
            if (rings.Count > 0)
            {
                polygons.Add(rings);
            }
        }

        return new GeoJsonGeometry
        {
            Type = "MultiPolygon",
            Coordinates = polygons
        };
    }

    /// <summary>
    /// Parsea una lista de coordenadas separadas por coma, soportando 2D (X Y), 3D (X Y Z), y espacios múltiples.
    /// </summary>
    private static List<double[]> ParseCoordinateList(string coordListStr)
    {
        var span = coordListStr.AsSpan();
        var result = new List<double[]>();

        int start = 0;
        for (int i = 0; i <= span.Length; i++)
        {
            if (i == span.Length || span[i] == ',')
            {
                var pairSpan = span.Slice(start, i - start).Trim();
                if (!pairSpan.IsEmpty)
                {
                    int pStart = 0;
                    ReadOnlySpan<char> lonSpan = default;
                    ReadOnlySpan<char> latSpan = default;

                    for (int j = 0; j <= pairSpan.Length; j++)
                    {
                        if (j == pairSpan.Length || char.IsWhiteSpace(pairSpan[j]))
                        {
                            var token = pairSpan.Slice(pStart, j - pStart).Trim();
                            if (!token.IsEmpty)
                            {
                                if (lonSpan.IsEmpty)
                                {
                                    lonSpan = token;
                                }
                                else if (latSpan.IsEmpty)
                                {
                                    latSpan = token;
                                    break; // Tomamos Lon y Lat; descartamos Z o tokens adicionales
                                }
                            }
                            pStart = j + 1;
                        }
                    }

                    if (!lonSpan.IsEmpty && !latSpan.IsEmpty)
                    {
                        if (double.TryParse(lonSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                            double.TryParse(latSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                        {
                            result.Add(new double[] { lon, lat });
                        }
                    }
                }
                start = i + 1;
            }
        }

        return result;
    }
}
