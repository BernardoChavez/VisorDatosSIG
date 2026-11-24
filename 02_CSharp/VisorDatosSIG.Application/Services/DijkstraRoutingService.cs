#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Agua;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Domain.Entities;

namespace VisorDatosSIG.Application.Services;

public class DijkstraRoutingService : IDijkstraRoutingService
{
    private readonly IAguaRepository _aguaRepository;

    public DijkstraRoutingService(IAguaRepository aguaRepository)
    {
        _aguaRepository = aguaRepository;
    }

    public async Task<RutaCalculadaDijkstraDto> CalcularRutaOptimaAsync(
        double partidaLon,
        double partidaLat,
        List<ConexionAguaDto> puntosDestino)
    {
        var resultado = new RutaCalculadaDijkstraDto();
        if (puntosDestino == null || puntosDestino.Count == 0)
        {
            resultado.RutaLineStringWkt = $"LINESTRING ({partidaLon.ToString("F6", CultureInfo.InvariantCulture)} {partidaLat.ToString("F6", CultureInfo.InvariantCulture)})";
            return resultado;
        }

        // 1. Obtener la red vial desde dbo.Vias
        var vias = await _aguaRepository.ObtenerRedVialAsync();
        var redVial = ConstruirGrafoVial(vias);

        // 2. Resolver orden secuencial (Nearest Neighbor sobre la red de vías)
        var inicioCoord = new Coord(partidaLon, partidaLat);
        var pendientes = puntosDestino.Select(p => new DestinoPunto(p)).ToList();

        var rutaCoordenadas = new List<Coord>();
        var puntosOrdenados = new List<PuntoRutaDijkstraDto>();
        decimal distanciaTotal = 0m;

        var nodoActual = redVial.ProyectarPuntoEnRed(inicioCoord);
        rutaCoordenadas.Add(inicioCoord);
        if (!inicioCoord.EsCasiIgual(nodoActual))
            rutaCoordenadas.Add(nodoActual);

        int secuencia = 1;
        while (pendientes.Count > 0)
        {
            // Encontrar el destino más cercano en la red vial desde el nodo actual
            double mejorDistancia = double.MaxValue;
            DestinoPunto? mejorDestino = null;
            List<Coord> mejorCamino = new();

            foreach (var candidato in pendientes)
            {
                var nodoDestino = redVial.ProyectarPuntoEnRed(candidato.Ubicacion);
                var (distancia, camino) = redVial.CalcularDijkstra(nodoActual, nodoDestino);

                // Agregar la distancia de acceso perpendicular al predio
                double distAcceso = HaversineMetros(nodoDestino, candidato.Ubicacion);
                double distTotalTramo = distancia + distAcceso;

                if (distTotalTramo < mejorDistancia)
                {
                    mejorDistancia = distTotalTramo;
                    mejorDestino = candidato;
                    mejorCamino = camino;
                }
            }

            if (mejorDestino == null) break;

            pendientes.Remove(mejorDestino);

            // Agregar el camino vial a la ruta total
            foreach (var c in mejorCamino)
            {
                if (rutaCoordenadas.Count == 0 || !rutaCoordenadas[^1].EsCasiIgual(c))
                {
                    rutaCoordenadas.Add(c);
                }
            }

            // Conectar el punto final del camino vial al medidor
            if (rutaCoordenadas.Count == 0 || !rutaCoordenadas[^1].EsCasiIgual(mejorDestino.Ubicacion))
            {
                rutaCoordenadas.Add(mejorDestino.Ubicacion);
            }

            decimal tramoM = Math.Round((decimal)mejorDistancia, 2);
            distanciaTotal += tramoM;

            puntosOrdenados.Add(new PuntoRutaDijkstraDto
            {
                OrdenSecuencia = secuencia++,
                IdConexion = mejorDestino.Conexion.IdConexion,
                CodigoConexion = mejorDestino.Conexion.CodigoConexion,
                Longitud = mejorDestino.Conexion.Longitud,
                Latitud = mejorDestino.Conexion.Latitud,
                DistanciaTramoMetros = tramoM
            });

            // El siguiente tramo parte desde la proyección vial del medidor
            nodoActual = redVial.ProyectarPuntoEnRed(mejorDestino.Ubicacion);
        }

        // 3. Generar WKT LINESTRING de toda la ruta
        resultado.DistanciaTotalMetros = distanciaTotal;
        resultado.PuntosOrdenados = puntosOrdenados;
        resultado.RutaLineStringWkt = ConstruirLineStringWkt(rutaCoordenadas);

        return resultado;
    }

    private static string ConstruirLineStringWkt(List<Coord> coords)
    {
        if (coords.Count == 0) return "LINESTRING EMPTY";
        if (coords.Count == 1)
            coords.Add(coords[0]); // Mínimo 2 puntos para un LINESTRING válido

        var sb = new StringBuilder("LINESTRING (");
        for (int i = 0; i < coords.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(coords[i].Lon.ToString("F6", CultureInfo.InvariantCulture));
            sb.Append(' ');
            sb.Append(coords[i].Lat.ToString("F6", CultureInfo.InvariantCulture));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private GrafoVial ConstruirGrafoVial(List<Via> vias)
    {
        var segmentos = new List<Segmento>();

        foreach (var via in vias)
        {
            var coords = ExtraerPuntosDeWkt(via.WktGeometria);
            for (int i = 0; i < coords.Count - 1; i++)
            {
                segmentos.Add(new Segmento(coords[i], coords[i + 1]));
            }
        }

        // Subdividir segmentos que se intersectan para generar nodos de esquina
        var segmentosFinales = SubdividirIntersecciones(segmentos);

        var grafo = new GrafoVial();
        foreach (var seg in segmentosFinales)
        {
            grafo.AgregarArista(seg.A, seg.B);
        }

        return grafo;
    }

    private static List<Coord> ExtraerPuntosDeWkt(string? wkt)
    {
        var lista = new List<Coord>();
        if (string.IsNullOrWhiteSpace(wkt)) return lista;

        // Extraer todos los pares de números en el LINESTRING
        var match = Regex.Match(wkt, @"\((.+)\)");
        if (!match.Success) return lista;

        var pares = match.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var par in pares)
        {
            var partes = par.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length >= 2 &&
                double.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                double.TryParse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
            {
                lista.Add(new Coord(lon, lat));
            }
        }

        return lista;
    }

    private static List<Segmento> SubdividirIntersecciones(List<Segmento> originales)
    {
        var resultado = new List<Segmento>(originales);
        bool huboDivision = true;
        int iteraciones = 0;

        while (huboDivision && iteraciones < 10)
        {
            huboDivision = false;
            iteraciones++;

            for (int i = 0; i < resultado.Count; i++)
            {
                for (int j = i + 1; j < resultado.Count; j++)
                {
                    var seg1 = resultado[i];
                    var seg2 = resultado[j];

                    if (seg1.CalcularInterseccionInterna(seg2, out Coord? pInt) && pInt != null)
                    {
                        var punto = pInt.Value;
                        // Reemplazar seg1 y seg2 por los submúltiplos
                        resultado.RemoveAt(j);
                        resultado.RemoveAt(i);

                        resultado.Add(new Segmento(seg1.A, punto));
                        resultado.Add(new Segmento(punto, seg1.B));
                        resultado.Add(new Segmento(seg2.A, punto));
                        resultado.Add(new Segmento(punto, seg2.B));

                        huboDivision = true;
                        break;
                    }
                }
                if (huboDivision) break;
            }
        }

        return resultado;
    }

    public static double HaversineMetros(Coord p1, Coord p2)
    {
        const double R = 6371000.0;
        double dLat = (p2.Lat - p1.Lat) * Math.PI / 180.0;
        double dLon = (p2.Lon - p1.Lon) * Math.PI / 180.0;

        double lat1 = p1.Lat * Math.PI / 180.0;
        double lat2 = p2.Lat * Math.PI / 180.0;

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return R * c;
    }

    private class DestinoPunto
    {
        public ConexionAguaDto Conexion { get; }
        public Coord Ubicacion { get; }

        public DestinoPunto(ConexionAguaDto c)
        {
            Conexion = c;
            Ubicacion = new Coord(c.Longitud, c.Latitud);
        }
    }
}

public readonly struct Coord : IEquatable<Coord>
{
    public double Lon { get; }
    public double Lat { get; }

    public Coord(double lon, double lat)
    {
        Lon = Math.Round(lon, 6);
        Lat = Math.Round(lat, 6);
    }

    public bool EsCasiIgual(Coord otra, double toleranciaMetros = 1.0)
    {
        return DijkstraRoutingService.HaversineMetros(this, otra) <= toleranciaMetros;
    }

    public bool Equals(Coord other) => Lon.Equals(other.Lon) && Lat.Equals(other.Lat);
    public override bool Equals(object? obj) => obj is Coord c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(Lon, Lat);
    public override string ToString() => $"({Lon:F6}, {Lat:F6})";
}

public class Segmento
{
    public Coord A { get; }
    public Coord B { get; }

    public Segmento(Coord a, Coord b)
    {
        A = a;
        B = b;
    }

    public bool CalcularInterseccionInterna(Segmento otro, out Coord? punto)
    {
        punto = null;
        double x1 = A.Lon, y1 = A.Lat;
        double x2 = B.Lon, y2 = B.Lat;
        double x3 = otro.A.Lon, y3 = otro.A.Lat;
        double x4 = otro.B.Lon, y4 = otro.B.Lat;

        double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        if (Math.Abs(denom) < 1e-11) return false;

        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

        // Estrictamente interno (no los extremos existentes)
        if (t > 0.001 && t < 0.999 && u > 0.001 && u < 0.999)
        {
            double ix = x1 + t * (x2 - x1);
            double iy = y1 + t * (y2 - y1);
            punto = new Coord(ix, iy);
            return true;
        }

        return false;
    }

    public Coord ProyectarPunto(Coord p)
    {
        double dx = B.Lon - A.Lon;
        double dy = B.Lat - A.Lat;
        double lenSq = dx * dx + dy * dy;

        if (lenSq < 1e-12) return A;

        double t = ((p.Lon - A.Lon) * dx + (p.Lat - A.Lat) * dy) / lenSq;
        t = Math.Clamp(t, 0.0, 1.0);

        return new Coord(A.Lon + t * dx, A.Lat + t * dy);
    }
}

public class GrafoVial
{
    private readonly Dictionary<Coord, List<(Coord Destino, double Distancia)>> _adj = new();
    private readonly List<Segmento> _segmentos = new();

    public void AgregarArista(Coord u, Coord v)
    {
        if (u.Equals(v)) return;

        double dist = DijkstraRoutingService.HaversineMetros(u, v);
        if (dist < 0.1) return;

        if (!_adj.ContainsKey(u)) _adj[u] = new();
        if (!_adj.ContainsKey(v)) _adj[v] = new();

        _adj[u].Add((v, dist));
        _adj[v].Add((u, dist));
        _segmentos.Add(new Segmento(u, v));
    }

    public Coord ProyectarPuntoEnRed(Coord p)
    {
        if (_segmentos.Count == 0) return p;

        Coord mejorProyeccion = p;
        double menorDistancia = double.MaxValue;

        foreach (var seg in _segmentos)
        {
            var proj = seg.ProyectarPunto(p);
            double d = DijkstraRoutingService.HaversineMetros(p, proj);
            if (d < menorDistancia)
            {
                menorDistancia = d;
                mejorProyeccion = proj;
            }
        }

        // Asegurar que la proyección esté conectada al grafo
        if (!_adj.ContainsKey(mejorProyeccion))
        {
            ConectarPuntoEnSegmentoCercano(mejorProyeccion);
        }

        return mejorProyeccion;
    }

    private void ConectarPuntoEnSegmentoCercano(Coord p)
    {
        Segmento? mejorSeg = null;
        double menorDist = double.MaxValue;

        foreach (var seg in _segmentos)
        {
            var proj = seg.ProyectarPunto(p);
            double d = DijkstraRoutingService.HaversineMetros(p, proj);
            if (d < menorDist)
            {
                menorDist = d;
                mejorSeg = seg;
            }
        }

        if (mejorSeg != null)
        {
            AgregarArista(mejorSeg.A, p);
            AgregarArista(p, mejorSeg.B);
        }
    }

    public (double Distancia, List<Coord> Camino) CalcularDijkstra(Coord origen, Coord destino)
    {
        if (origen.Equals(destino))
            return (0.0, new List<Coord> { origen });

        if (!_adj.ContainsKey(origen)) ConectarPuntoEnSegmentoCercano(origen);
        if (!_adj.ContainsKey(destino)) ConectarPuntoEnSegmentoCercano(destino);

        var distancias = new Dictionary<Coord, double>();
        var previo = new Dictionary<Coord, Coord>();
        var cola = new PriorityQueue<Coord, double>();

        distancias[origen] = 0;
        cola.Enqueue(origen, 0);

        while (cola.Count > 0)
        {
            var actual = cola.Dequeue();
            if (actual.Equals(destino)) break;

            if (!_adj.TryGetValue(actual, out var vecinos)) continue;

            double distActual = distancias[actual];
            foreach (var (vecino, peso) in vecinos)
            {
                double nuevaDist = distActual + peso;
                if (!distancias.TryGetValue(vecino, out double mejorHastaAhora) || nuevaDist < mejorHastaAhora)
                {
                    distancias[vecino] = nuevaDist;
                    previo[vecino] = actual;
                    cola.Enqueue(vecino, nuevaDist);
                }
            }
        }

        if (!distancias.ContainsKey(destino))
        {
            // Si por alguna razón estuviera desconectado, retornar conexión directa
            double dDirecta = DijkstraRoutingService.HaversineMetros(origen, destino);
            return (dDirecta, new List<Coord> { origen, destino });
        }

        // Reconstruir camino
        var camino = new List<Coord>();
        var curr = destino;
        while (!curr.Equals(origen))
        {
            camino.Add(curr);
            curr = previo[curr];
        }
        camino.Add(origen);
        camino.Reverse();

        return (distancias[destino], camino);
    }
}
