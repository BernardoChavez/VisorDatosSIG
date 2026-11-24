import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import '../models/punto_tarea.dart';
import '../models/asignacion.dart';
import '../services/location_service.dart';
import '../services/navigation_service.dart';
import 'field_record_screen.dart';

class TaskDetailNavigationScreen extends StatefulWidget {
  final PuntoTarea punto;
  final Asignacion asignacion;

  const TaskDetailNavigationScreen({
    super.key,
    required this.punto,
    required this.asignacion,
  });

  @override
  State<TaskDetailNavigationScreen> createState() =>
      _TaskDetailNavigationScreenState();
}

class _TaskDetailNavigationScreenState
    extends State<TaskDetailNavigationScreen> {
  Position? _posicionActual;
  double? _distanciaMetros;
  bool _obteniendoGps = false;

  @override
  void initState() {
    super.initState();
    _actualizarUbicacionGps();
  }

  Future<void> _actualizarUbicacionGps() async {
    setState(() {
      _obteniendoGps = true;
    });

    final pos = await LocationService.obtenerPosicionActual();

    if (!mounted) return;

    if (pos != null) {
      final dist = LocationService.calcularDistanciaMetros(
        latOrigen: pos.latitude,
        lonOrigen: pos.longitude,
        latDestino: widget.punto.latitud,
        lonDestino: widget.punto.longitud,
      );

      setState(() {
        _posicionActual = pos;
        _distanciaMetros = dist;
        _obteniendoGps = false;
      });
    } else {
      setState(() {
        _obteniendoGps = false;
      });
    }
  }

  void _simularUbicacionCercana() {
    // Simula estar a ~10 metros del medidor para pruebas en interiores o laboratorio
    final simPos = Position(
      longitude: widget.punto.longitud + 0.00008,
      latitude: widget.punto.latitud + 0.00008,
      timestamp: DateTime.now(),
      accuracy: 3.5,
      altitude: 400.0,
      altitudeAccuracy: 1.0,
      heading: 90.0,
      headingAccuracy: 1.0,
      speed: 0.0,
      speedAccuracy: 0.0,
    );

    final dist = LocationService.calcularDistanciaMetros(
      latOrigen: simPos.latitude,
      lonOrigen: simPos.longitude,
      latDestino: widget.punto.latitud,
      lonDestino: widget.punto.longitud,
    );

    setState(() {
      _posicionActual = simPos;
      _distanciaMetros = dist;
      LocationService.ultimaPosicion = simPos;
    });

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Ubicación simulada a ${dist.toStringAsFixed(1)} metros del medidor (Válido para prueba).',
        ),
        backgroundColor: Colors.indigo,
      ),
    );
  }

  void _abrirModalNavegacion() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Seleccione Modo de Navegación Giro a Giro',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 6),
              const Text(
                'Se abrirá Google Maps con instrucciones de voz y ruta óptima:',
                style: TextStyle(fontSize: 13, color: Colors.black54),
              ),
              const SizedBox(height: 16),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: Colors.amber.shade100,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.two_wheeler, color: Colors.amber),
                ),
                title: const Text('En Motocicleta', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('Ruta recomendada para operarios en moto'),
                onTap: () {
                  Navigator.pop(ctx);
                  NavigationService.abrirNavegacionGiroAGiro(
                    latitudDestino: widget.punto.latitud,
                    longitudDestino: widget.punto.longitud,
                    tituloPunto: 'Punto #${widget.punto.ordenSecuencia} - ${widget.punto.titular}',
                    modo: ModoNavegacion.moto,
                  );
                },
              ),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: Colors.blue.shade100,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.directions_car, color: Colors.blue),
                ),
                title: const Text('En Vehículo / Camioneta', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('Ruta para automóvil respetando calles'),
                onTap: () {
                  Navigator.pop(ctx);
                  NavigationService.abrirNavegacionGiroAGiro(
                    latitudDestino: widget.punto.latitud,
                    longitudDestino: widget.punto.longitud,
                    tituloPunto: 'Punto #${widget.punto.ordenSecuencia} - ${widget.punto.titular}',
                    modo: ModoNavegacion.vehiculo,
                  );
                },
              ),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: Colors.green.shade100,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.directions_walk, color: Colors.green),
                ),
                title: const Text('A pie / Caminando', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('Navegación peatonal paso a paso'),
                onTap: () {
                  Navigator.pop(ctx);
                  NavigationService.abrirNavegacionGiroAGiro(
                    latitudDestino: widget.punto.latitud,
                    longitudDestino: widget.punto.longitud,
                    tituloPunto: 'Punto #${widget.punto.ordenSecuencia} - ${widget.punto.titular}',
                    modo: ModoNavegacion.aPie,
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final punto = widget.punto;
    final enRangoValido = _distanciaMetros != null && _distanciaMetros! <= 60.0;

    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9),
      appBar: AppBar(
        title: Text('Parada #${punto.ordenSecuencia} de ${widget.asignacion.puntos.length}'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Tarjeta de Información de la Conexión
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            elevation: 2,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.blue.shade700,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          'Secuencia #${punto.ordenSecuencia}',
                          style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                            fontSize: 13,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.grey.shade200,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          punto.codigoConexion,
                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12),
                        ),
                      ),
                      const Spacer(),
                      _buildEstadoPill(punto.estado),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    punto.titular,
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 4),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.location_on, size: 16, color: Colors.redAccent),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          punto.direccion,
                          style: const TextStyle(fontSize: 13, color: Colors.black87),
                        ),
                      ),
                    ],
                  ),
                  const Divider(height: 24),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _buildDatoItem('Medidor', punto.numeroMedidor, Icons.speed),
                      _buildDatoItem('Lote', punto.codigoLote ?? 'S/N', Icons.domain),
                      _buildDatoItem('Manzana', punto.codigoManzana ?? 'S/N', Icons.grid_view),
                      _buildDatoItem(
                        'Lectura Ant.',
                        punto.lecturaAnterior != null
                            ? '${punto.lecturaAnterior!.toStringAsFixed(1)} m³'
                            : 'N/A',
                        Icons.history,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Tarjeta de GPS y Geolocalización en Tiempo Real
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            elevation: 2,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Row(
                        children: [
                          Icon(Icons.gps_fixed, color: Colors.blueAccent),
                          SizedBox(width: 8),
                          Text(
                            'Georreferenciación GPS',
                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      IconButton(
                        icon: _obteniendoGps
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.refresh, color: Colors.blue),
                        tooltip: 'Actualizar GPS',
                        onPressed: _obteniendoGps ? null : _actualizarUbicacionGps,
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),

                  // Coordenadas Destino
                  _buildFilaCoordenada(
                    'Destino (Medidor):',
                    'Lat: ${punto.latitud.toStringAsFixed(5)}, Lon: ${punto.longitud.toStringAsFixed(5)}',
                    Icons.flag,
                    Colors.red,
                  ),
                  const SizedBox(height: 8),

                  // Coordenadas Operario
                  _buildFilaCoordenada(
                    'Tu Ubicación Actual:',
                    _posicionActual != null
                        ? 'Lat: ${_posicionActual!.latitude.toStringAsFixed(5)}, Lon: ${_posicionActual!.longitude.toStringAsFixed(5)} (±${_posicionActual!.accuracy.toStringAsFixed(1)}m)'
                        : 'GPS no obtenido aún',
                    Icons.my_location,
                    Colors.blue,
                  ),
                  const SizedBox(height: 14),

                  // Distancia en tiempo real
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: enRangoValido ? Colors.green.shade50 : Colors.amber.shade50,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: enRangoValido ? Colors.green.shade300 : Colors.amber.shade300,
                      ),
                    ),
                    child: Column(
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              enRangoValido ? Icons.verified : Icons.warning_amber_rounded,
                              color: enRangoValido ? Colors.green.shade800 : Colors.amber.shade900,
                            ),
                            const SizedBox(width: 8),
                            Text(
                              _distanciaMetros != null
                                  ? '${_distanciaMetros!.toStringAsFixed(1)} metros de distancia'
                                  : 'Calculando distancia...',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                                color: enRangoValido ? Colors.green.shade900 : Colors.amber.shade900,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text(
                          enRangoValido
                              ? '✓ Ubicación válida dentro de los 60 metros reglamentarios.'
                              : 'Tolerancia máxima permitida para registrar: 60 metros.',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 11,
                            color: enRangoValido ? Colors.green.shade800 : Colors.amber.shade900,
                          ),
                        ),
                      ],
                    ),
                  ),

                  // Botón de prueba / simulación de cercanía
                  const SizedBox(height: 8),
                  Align(
                    alignment: Alignment.centerRight,
                    child: TextButton.icon(
                      onPressed: _simularUbicacionCercana,
                      icon: const Icon(Icons.pin_drop, size: 14),
                      label: const Text('Simular llegada al medidor (Pruebas)', style: TextStyle(fontSize: 11)),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),

          // Botón 1: "Navegar con Instrucciones Giro a Giro"
          ElevatedButton.icon(
            onPressed: _abrirModalNavegacion,
            icon: const Icon(Icons.navigation, color: Colors.white),
            label: const Text(
              'Navegar (Instrucciones Giro a Giro)',
              style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Colors.white),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.indigo.shade600,
              padding: const EdgeInsets.symmetric(vertical: 14),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
          ),
          const SizedBox(height: 12),

          // Botón 2: "Registrar en Campo (Foto, Lectura, Observación)"
          ElevatedButton.icon(
            onPressed: () async {
              final guardado = await Navigator.push<bool>(
                context,
                MaterialPageRoute(
                  builder: (_) => FieldRecordScreen(
                    punto: punto,
                    asignacion: widget.asignacion,
                    posicionGps: _posicionActual,
                  ),
                ),
              );

              if (guardado == true && mounted) {
                Navigator.pop(context, true);
              }
            },
            icon: const Icon(Icons.camera_alt, color: Colors.white),
            label: Text(
              punto.estaCompletado
                  ? 'Ver / Re-registrar Ejecución'
                  : 'Registrar Corte en Campo',
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Colors.white),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: punto.estaCompletado ? Colors.teal : Colors.blue.shade700,
              padding: const EdgeInsets.symmetric(vertical: 14),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDatoItem(String label, String valor, IconData icono) {
    return Column(
      children: [
        Icon(icono, size: 16, color: Colors.blueGrey),
        const SizedBox(height: 2),
        Text(valor, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
        Text(label, style: const TextStyle(fontSize: 10, color: Colors.black54)),
      ],
    );
  }

  Widget _buildFilaCoordenada(String label, String valor, IconData icono, Color color) {
    return Row(
      children: [
        Icon(icono, size: 16, color: color),
        const SizedBox(width: 8),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: const TextStyle(fontSize: 11, color: Colors.black54)),
            Text(valor, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
          ],
        ),
      ],
    );
  }

  Widget _buildEstadoPill(String estado) {
    Color color = Colors.orange;
    if (estado == 'Ejecutado') color = Colors.green;
    if (estado == 'NoRealizado') color = Colors.red;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withOpacity(0.15),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: color),
      ),
      child: Text(
        estado,
        style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: color),
      ),
    );
  }
}
