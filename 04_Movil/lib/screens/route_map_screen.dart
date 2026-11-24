import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import '../models/asignacion.dart';
import '../models/punto_tarea.dart';
import '../services/location_service.dart';
import 'task_detail_navigation_screen.dart';

class RouteMapScreen extends StatefulWidget {
  final Asignacion asignacion;

  const RouteMapScreen({super.key, required this.asignacion});

  @override
  State<RouteMapScreen> createState() => _RouteMapScreenState();
}

class _RouteMapScreenState extends State<RouteMapScreen> {
  final MapController _mapController = MapController();
  Position? _posicionActual;
  List<LatLng> _puntosPolilinea = [];
  LatLng _centroMapa = const LatLng(-12.0935, -77.0345);

  @override
  void initState() {
    super.initState();
    _procesarRuta();
    _obtenerGpsActual();
  }

  void _procesarRuta() {
    // 1. Extraer polilínea de la ruta óptima calculada por Dijkstra
    _puntosPolilinea = _parseWktLineString(widget.asignacion.rutaOptimaWkt);

    // 2. Si no hay WKT, conectar los puntos ordenados secuencialmente
    if (_puntosPolilinea.isEmpty && widget.asignacion.puntos.isNotEmpty) {
      _puntosPolilinea.add(
        LatLng(widget.asignacion.puntoPartidaLat, widget.asignacion.puntoPartidaLon),
      );
      for (var p in widget.asignacion.puntos) {
        _puntosPolilinea.add(LatLng(p.latitud, p.longitud));
      }
    }

    // 3. Determinar el centro del mapa
    if (widget.asignacion.puntos.isNotEmpty) {
      final primerPunto = widget.asignacion.puntos.first;
      _centroMapa = LatLng(primerPunto.latitud, primerPunto.longitud);
    }
  }

  List<LatLng> _parseWktLineString(String? wkt) {
    if (wkt == null || !wkt.toUpperCase().contains('LINESTRING')) return [];
    try {
      final start = wkt.indexOf('(');
      final end = wkt.lastIndexOf(')');
      if (start == -1 || end == -1) return [];
      final inner = wkt.substring(start + 1, end).replaceAll('(', '').replaceAll(')', '');
      final pairs = inner.split(',');
      List<LatLng> points = [];
      for (var pair in pairs) {
        final parts = pair.trim().split(RegExp(r'\s+'));
        if (parts.length >= 2) {
          final lon = double.tryParse(parts[0]);
          final lat = double.tryParse(parts[1]);
          if (lon != null && lat != null) {
            points.add(LatLng(lat, lon));
          }
        }
      }
      return points;
    } catch (_) {
      return [];
    }
  }

  Future<void> _obtenerGpsActual() async {
    final pos = await LocationService.obtenerPosicionActual();
    if (mounted && pos != null) {
      setState(() => _posicionActual = pos);
    }
  }

  void _mostrarDetallePunto(PuntoTarea punto) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  CircleAvatar(
                    backgroundColor: Colors.blue.shade700,
                    radius: 16,
                    child: Text(
                      '#${punto.ordenSecuencia}',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      punto.titular,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: punto.estaCompletado ? Colors.green.shade50 : Colors.amber.shade50,
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      punto.estado,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: punto.estaCompletado ? Colors.green.shade800 : Colors.amber.shade900,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                'Dirección: ${punto.direccion}',
                style: const TextStyle(fontSize: 13, color: Colors.black87),
              ),
              const SizedBox(height: 4),
              Text(
                'Medidor: ${punto.numeroMedidor} • Lote: ${punto.codigoLote ?? "S/N"}',
                style: const TextStyle(fontSize: 12, color: Colors.black54),
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: () {
                    Navigator.pop(ctx);
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => TaskDetailNavigationScreen(
                          punto: punto,
                          asignacion: widget.asignacion,
                        ),
                      ),
                    );
                  },
                  icon: const Icon(Icons.navigation, color: Colors.white),
                  label: const Text(
                    'Ver Navegación y Registrar',
                    style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.blue.shade700,
                    padding: const EdgeInsets.symmetric(vertical: 12),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Mapa de Ruta: ${widget.asignacion.codigoAsignacion}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.my_location),
            tooltip: 'Centrar en mi ubicación',
            onPressed: () {
              if (_posicionActual != null) {
                _mapController.move(
                  LatLng(_posicionActual!.latitude, _posicionActual!.longitude),
                  16.5,
                );
              } else {
                _obtenerGpsActual();
              }
            },
          ),
        ],
      ),
      body: FlutterMap(
        mapController: _mapController,
        options: MapOptions(
          initialCenter: _centroMapa,
          initialZoom: 16.0,
        ),
        children: [
          // Capa de teselas OpenStreetMap
          TileLayer(
            urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            userAgentPackageName: 'com.uagrm.sig.visor_datos_sig_movil',
          ),

          // Capa de Ruta Óptima (Polilínea trazada por Dijkstra respetando calles)
          if (_puntosPolilinea.isNotEmpty)
            PolylineLayer(
              polylines: [
                Polyline(
                  points: _puntosPolilinea,
                  strokeWidth: 4.5,
                  color: Colors.blue.shade700.withOpacity(0.85),
                ),
              ],
            ),

          // Capa de Marcadores (Partida, Puntos Secuenciales 1, 2, 3... y Operario)
          MarkerLayer(
            markers: [
              // Marcador del Punto de Partida
              Marker(
                point: LatLng(widget.asignacion.puntoPartidaLat, widget.asignacion.puntoPartidaLon),
                width: 36,
                height: 36,
                child: const CircleAvatar(
                  backgroundColor: Colors.black87,
                  child: Icon(Icons.warehouse, color: Colors.white, size: 20),
                ),
              ),

              // Marcadores de las paradas ordenadas por Dijkstra (1, 2, 3...)
              ...widget.asignacion.puntos.map((p) {
                Color mColor = Colors.blue.shade700;
                if (p.estado == 'Ejecutado') mColor = Colors.green;
                if (p.estado == 'NoRealizado') mColor = Colors.red;

                return Marker(
                  point: LatLng(p.latitud, p.longitud),
                  width: 38,
                  height: 38,
                  child: GestureDetector(
                    onTap: () => _mostrarDetallePunto(p),
                    child: Container(
                      decoration: BoxDecoration(
                        color: mColor,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.white, width: 2),
                        boxShadow: const [
                          BoxShadow(
                            color: Colors.black26,
                            blurRadius: 4,
                            offset: Offset(0, 2),
                          ),
                        ],
                      ),
                      child: Center(
                        child: Text(
                          '${p.ordenSecuencia}',
                          style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                          ),
                        ),
                      ),
                    ),
                  ),
                );
              }),

              // Marcador de Ubicación Actual del Operario
              if (_posicionActual != null)
                Marker(
                  point: LatLng(_posicionActual!.latitude, _posicionActual!.longitude),
                  width: 28,
                  height: 28,
                  child: Container(
                    decoration: BoxDecoration(
                      color: Colors.cyanAccent.shade700,
                      shape: BoxShape.circle,
                      border: Border.all(color: Colors.white, width: 3),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.cyan.withOpacity(0.5),
                          blurRadius: 8,
                          spreadRadius: 2,
                        ),
                      ],
                    ),
                    child: const Icon(Icons.person_pin_circle, color: Colors.white, size: 16),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}
