import 'dart:async';
import 'package:geolocator/geolocator.dart';
import 'api_service.dart';

class LocationService {
  static Position? ultimaPosicion;
  static Timer? _telemetriaTimer;
  static int? _idTrabajadorActivo;

  // Verificar y solicitar permisos de GPS
  static Future<bool> verificarPermisos() async {
    bool servicioHabilitado = await Geolocator.isLocationServiceEnabled();
    if (!servicioHabilitado) {
      return false;
    }

    LocationPermission permiso = await Geolocator.checkPermission();
    if (permiso == LocationPermission.denied) {
      permiso = await Geolocator.requestPermission();
      if (permiso == LocationPermission.denied) {
        return false;
      }
    }

    if (permiso == LocationPermission.deniedForever) {
      return false;
    }

    return true;
  }

  // Obtener ubicación GPS actual con alta precisión
  static Future<Position?> obtenerPosicionActual() async {
    try {
      final tienePermiso = await verificarPermisos();
      if (!tienePermiso) {
        return ultimaPosicion;
      }

      final pos = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
        timeLimit: const Duration(seconds: 10),
      );
      ultimaPosicion = pos;
      return pos;
    } catch (e) {
      print('Error al obtener GPS: $e');
      return ultimaPosicion;
    }
  }

  // Calcular distancia geodésica en metros entre 2 puntos
  static double calcularDistanciaMetros({
    required double latOrigen,
    required double lonOrigen,
    required double latDestino,
    required double lonDestino,
  }) {
    return Geolocator.distanceBetween(latOrigen, lonOrigen, latDestino, lonDestino);
  }

  // Iniciar telemetría periódica hacia el panel de monitoreo (cada 12 segundos)
  static void iniciarTelemetria(int idTrabajador) {
    detenerTelemetria();
    _idTrabajadorActivo = idTrabajador;

    // Enviar primer ping inmediato
    _enviarPing();

    // Configurar temporizador recurrente
    _telemetriaTimer = Timer.periodic(const Duration(seconds: 12), (timer) {
      _enviarPing();
    });
  }

  static void detenerTelemetria() {
    _telemetriaTimer?.cancel();
    _telemetriaTimer = null;
    _idTrabajadorActivo = null;
  }

  static Future<void> _enviarPing() async {
    if (_idTrabajadorActivo == null) return;
    try {
      final pos = await obtenerPosicionActual();
      if (pos != null) {
        await ApiService.enviarTelemetriaUbicacion(
          idTrabajador: _idTrabajadorActivo!,
          latitud: pos.latitude,
          longitud: pos.longitude,
          velocidadKmH: pos.speed * 3.6, // m/s a km/h
          rumbo: pos.heading,
          precisionM: pos.accuracy,
          bateriaPorcentaje: 92,
        );
      }
    } catch (_) {}
  }
}
