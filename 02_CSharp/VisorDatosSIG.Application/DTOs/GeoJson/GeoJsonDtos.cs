#nullable enable
using System.Collections.Generic;

namespace VisorDatosSIG.Application.DTOs.GeoJson;

/// <summary>
/// Representa una colección GeoJSON estándar (RFC 7946).
/// Se serializa automáticamente en camelCase ("type", "features", "bbox").
/// </summary>
public class GeoJsonFeatureCollection
{
    public string Type { get; set; } = "FeatureCollection";
    public List<GeoJsonFeature> Features { get; set; } = new();
    public int TotalFeatures => Features.Count;
    public double[]? Bbox { get; set; }
}

/// <summary>
/// Representa una característica individual GeoJSON.
/// </summary>
public class GeoJsonFeature
{
    public string Type { get; set; } = "Feature";
    public object? Id { get; set; }
    public GeoJsonGeometry? Geometry { get; set; }
    public Dictionary<string, object?> Properties { get; set; } = new();
}

/// <summary>
/// Representa la geometría de una característica GeoJSON.
/// </summary>
public class GeoJsonGeometry
{
    public string Type { get; set; } = "Polygon"; // Point, LineString, Polygon, MultiPolygon, MultiLineString, MultiPoint
    public object? Coordinates { get; set; }
}
