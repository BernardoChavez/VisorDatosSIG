#nullable enable
using System;
using System.Globalization;

namespace VisorDatosSIG.Domain.ValueObjects;

/// <summary>
/// Representa un punto geográfico con longitud y latitud en WGS 84 (SRID 4326).
/// </summary>
public record Coordenada
{
    public double Longitud { get; init; }
    public double Latitud { get; init; }

    public Coordenada(double longitud, double latitud)
    {
        if (longitud < -180.0 || longitud > 180.0)
            throw new ArgumentOutOfRangeException(nameof(longitud), "La longitud debe estar entre -180 y 180 grados.");

        if (latitud < -90.0 || latitud > 90.0)
            throw new ArgumentOutOfRangeException(nameof(latitud), "La latitud debe estar entre -90 y 90 grados.");

        Longitud = longitud;
        Latitud = latitud;
    }

    public string ToWktPoint() =>
        string.Format(CultureInfo.InvariantCulture, "POINT({0} {1})", Longitud, Latitud);

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "({0}, {1})", Longitud, Latitud);
}

/// <summary>
/// Representa una caja envolvente o ventana espacial (Bounding Box) en WGS 84.
/// </summary>
public record BoundingBox
{
    public double MinLon { get; init; }
    public double MinLat { get; init; }
    public double MaxLon { get; init; }
    public double MaxLat { get; init; }

    public BoundingBox(double minLon, double minLat, double maxLon, double maxLat)
    {
        if (minLon < -180.0 || minLon > 180.0 || maxLon < -180.0 || maxLon > 180.0)
            throw new ArgumentOutOfRangeException("Las longitudes deben ubicarse entre -180 y 180 grados.");

        if (minLat < -90.0 || minLat > 90.0 || maxLat < -90.0 || maxLat > 90.0)
            throw new ArgumentOutOfRangeException("Las latitudes deben ubicarse entre -90 y 90 grados.");

        if (minLon > maxLon)
            throw new ArgumentException("MinLon no puede ser mayor que MaxLon.", nameof(minLon));

        if (minLat > maxLat)
            throw new ArgumentException("MinLat no puede ser mayor que MaxLat.", nameof(minLat));

        MinLon = minLon;
        MinLat = minLat;
        MaxLon = maxLon;
        MaxLat = maxLat;
    }

    public double CentroLon => (MinLon + MaxLon) / 2.0;
    public double CentroLat => (MinLat + MaxLat) / 2.0;

    /// <summary>
    /// Genera la representación WKT del polígono rectangular correspondiente al BBox.
    /// </summary>
    public string ToWktPolygon()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "POLYGON(({0} {1}, {2} {1}, {2} {3}, {0} {3}, {0} {1}))",
            MinLon, MinLat, MaxLon, MaxLat);
    }

    /// <summary>
    /// Determina si una coordenada se encuentra contenida en la caja envolvente.
    /// </summary>
    public bool Contiene(double lon, double lat)
    {
        return lon >= MinLon && lon <= MaxLon && lat >= MinLat && lat <= MaxLat;
    }

    public static BoundingBox DesdeCadena(string bboxStr)
    {
        if (string.IsNullOrWhiteSpace(bboxStr))
            throw new ArgumentException("La cadena BBox no puede estar vacía.", nameof(bboxStr));

        var partes = bboxStr.Split(',');
        if (partes.Length != 4)
            throw new FormatException("El formato de BBox debe ser 'minLon,minLat,maxLon,maxLat'.");

        var minLon = double.Parse(partes[0].Trim(), CultureInfo.InvariantCulture);
        var minLat = double.Parse(partes[1].Trim(), CultureInfo.InvariantCulture);
        var maxLon = double.Parse(partes[2].Trim(), CultureInfo.InvariantCulture);
        var maxLat = double.Parse(partes[3].Trim(), CultureInfo.InvariantCulture);

        return new BoundingBox(minLon, minLat, maxLon, maxLat);
    }
}
