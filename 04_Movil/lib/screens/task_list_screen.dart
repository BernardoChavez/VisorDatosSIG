import 'package:flutter/material.dart';
import '../models/asignacion.dart';
import '../models/punto_tarea.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import 'login_screen.dart';
import 'task_detail_navigation_screen.dart';
import 'route_map_screen.dart';

class TaskListScreen extends StatefulWidget {
  const TaskListScreen({super.key});

  @override
  State<TaskListScreen> createState() => _TaskListScreenState();
}

class _TaskListScreenState extends State<TaskListScreen> {
  Asignacion? _asignacion;
  bool _cargando = true;
  String? _mensajeError;

  @override
  void initState() {
    super.initState();
    _cargarAsignacion();
  }

  Future<void> _cargarAsignacion() async {
    final trabajador = AuthService.trabajadorActual;
    if (trabajador == null) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const LoginScreen()),
      );
      return;
    }

    setState(() {
      _cargando = true;
      _mensajeError = null;
    });

    final asig = await ApiService.obtenerAsignacionActiva(trabajador.idTrabajador);

    if (!mounted) return;

    setState(() {
      _asignacion = asig;
      _cargando = false;
      if (asig == null) {
        _mensajeError = 'No tiene rutas de corte o reconexión asignadas en este momento.';
      }
    });
  }

  Future<void> _cerrarSesion() async {
    final confirmar = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cerrar Sesión'),
        content: const Text('¿Está seguro de que desea salir de la aplicación?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancelar'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Salir', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirmar == true) {
      await AuthService.cerrarSesion();
      if (!mounted) return;
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const LoginScreen()),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final trabajador = AuthService.trabajadorActual;

    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9),
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              trabajador?.nombreCompleto ?? 'Operario',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: trabajador?.tipoTrabajador == 'Cortador'
                        ? Colors.amber.shade700
                        : Colors.green.shade700,
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Text(
                    trabajador?.tipoTrabajador ?? 'Operario',
                    style: const TextStyle(fontSize: 10, color: Colors.white, fontWeight: FontWeight.bold),
                  ),
                ),
                const SizedBox(width: 6),
                Text(
                  '${trabajador?.codigoTrabajador} • ${trabajador?.vehiculo ?? 'A pie'}',
                  style: const TextStyle(fontSize: 11, color: Colors.white70),
                ),
              ],
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Actualizar Tareas',
            onPressed: _cargarAsignacion,
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Cerrar Sesión',
            onPressed: _cerrarSesion,
          ),
        ],
      ),
      body: _cargando
          ? const Center(child: CircularProgressIndicator())
          : _asignacion == null
              ? _buildSinAsignacion()
              : _buildContenidoAsignacion(),
      floatingActionButton: _asignacion != null && _asignacion!.puntos.isNotEmpty
          ? FloatingActionButton.extended(
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => RouteMapScreen(asignacion: _asignacion!),
                  ),
                );
              },
              backgroundColor: Colors.blue.shade700,
              icon: const Icon(Icons.map, color: Colors.white),
              label: const Text(
                'Ver Ruta en Mapa',
                style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
              ),
            )
          : null,
    );
  }

  Widget _buildSinAsignacion() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.assignment_turned_in_outlined, size: 72, color: Colors.grey.shade400),
            const SizedBox(height: 16),
            const Text(
              'Sin Tareas Asignadas',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.blueGrey),
            ),
            const SizedBox(height: 8),
            Text(
              _mensajeError ?? 'Comuníquese con el Administrador para que le asigne una ruta de corte/reconexión optimizada con Dijkstra.',
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 20),
            ElevatedButton.icon(
              onPressed: _cargarAsignacion,
              icon: const Icon(Icons.refresh),
              label: const Text('Reintentar'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildContenidoAsignacion() {
    final asig = _asignacion!;
    final total = asig.puntos.length;
    final completados = asig.puntos.where((p) => p.estaCompletado).length;
    final pendientes = total - completados;
    final porcentaje = total > 0 ? (completados / total) : 0.0;

    return RefreshIndicator(
      onRefresh: _cargarAsignacion,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 80),
        children: [
          // Tarjeta de Resumen de Ruta Dijkstra
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            elevation: 2,
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          Icon(
                            asig.tipoServicio == 'Corte'
                                ? Icons.content_cut
                                : Icons.power_input,
                            color: asig.tipoServicio == 'Corte' ? Colors.red : Colors.green,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Ruta de ${asig.tipoServicio}',
                            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.blue.shade50,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          asig.codigoAsignacion,
                          style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.blue.shade800),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Partida: ${asig.puntoPartidaNombre}',
                    style: const TextStyle(fontSize: 12, color: Colors.black54),
                  ),
                  const SizedBox(height: 12),
                  // Barra de Progreso
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: LinearProgressIndicator(
                      value: porcentaje,
                      minHeight: 10,
                      backgroundColor: Colors.grey.shade200,
                      color: Colors.blue.shade600,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _buildMiniKpi('Total', '$total', Colors.blueGrey),
                      _buildMiniKpi('Completados', '$completados', Colors.green),
                      _buildMiniKpi('Pendientes', '$pendientes', Colors.orange),
                      _buildMiniKpi('Distancia', '${(asig.distanciaTotalMetros / 1000).toStringAsFixed(1)} km', Colors.indigo),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Secuencia Óptima de Puntos (1, 2, 3...)',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: Color(0xFF334155)),
              ),
              Text(
                '$pendientes pendientes',
                style: const TextStyle(fontSize: 12, color: Colors.black54),
              ),
            ],
          ),
          const SizedBox(height: 8),

          // Lista de Puntos Ordenados Secuencialmente por Dijkstra
          ...asig.puntos.map((punto) => _buildCardPunto(punto, asig)),
        ],
      ),
    );
  }

  Widget _buildMiniKpi(String label, String valor, Color color) {
    return Column(
      children: [
        Text(
          valor,
          style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: color),
        ),
        const SizedBox(height: 2),
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: Colors.black54),
        ),
      ],
    );
  }

  Widget _buildCardPunto(PuntoTarea punto, Asignacion asignacion) {
    Color badgeColor;

    if (punto.estado == 'Ejecutado') {
      badgeColor = Colors.green;
    } else if (punto.estado == 'NoRealizado') {
      badgeColor = Colors.red;
    } else {
      badgeColor = Colors.blue.shade700;
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      elevation: 1,
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () async {
          final actualizado = await Navigator.push<bool>(
            context,
            MaterialPageRoute(
              builder: (_) => TaskDetailNavigationScreen(
                punto: punto,
                asignacion: asignacion,
              ),
            ),
          );
          if (actualizado == true) {
            _cargarAsignacion();
          }
        },
        child: Padding(
          padding: const EdgeInsets.all(12.0),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Badge con el Número de Secuencia (1, 2, 3...)
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: badgeColor.withOpacity(0.12),
                  shape: BoxShape.circle,
                  border: Border.all(color: badgeColor, width: 1.5),
                ),
                child: Center(
                  child: Text(
                    '#${punto.ordenSecuencia}',
                    style: TextStyle(
                      color: badgeColor,
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              // Datos del cliente y conexión
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            punto.titular,
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              decoration: punto.estaCompletado
                                  ? TextDecoration.lineThrough
                                  : TextDecoration.none,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        _buildEstadoChip(punto.estado),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      punto.direccion,
                      style: const TextStyle(fontSize: 12, color: Colors.black87),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        Icon(Icons.speed, size: 13, color: Colors.blue.shade700),
                        const SizedBox(width: 4),
                        Text(
                          'Medidor: ${punto.numeroMedidor}',
                          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: Colors.black54),
                        ),
                        const SizedBox(width: 12),
                        Icon(Icons.place_outlined, size: 13, color: Colors.blueGrey),
                        const SizedBox(width: 2),
                        Text(
                          'Lote: ${punto.codigoLote ?? "S/N"}',
                          style: const TextStyle(fontSize: 11, color: Colors.black54),
                        ),
                        const Spacer(),
                        Text(
                          '${punto.distanciaDesdeAnteriorM.toStringAsFixed(0)} m',
                          style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: Colors.indigo.shade600),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right, color: Colors.grey),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildEstadoChip(String estado) {
    Color bg;
    Color fg;
    String label;

    switch (estado) {
      case 'Ejecutado':
        bg = Colors.green.shade50;
        fg = Colors.green.shade800;
        label = 'Cortado / Listo';
        break;
      case 'NoRealizado':
        bg = Colors.red.shade50;
        fg = Colors.red.shade800;
        label = 'Casa Cerrada';
        break;
      default:
        bg = Colors.amber.shade50;
        fg = Colors.amber.shade900;
        label = 'Pendiente';
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: fg),
      ),
    );
  }
}
