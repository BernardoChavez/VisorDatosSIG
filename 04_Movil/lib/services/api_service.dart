import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import '../config/api_config.dart';
import '../models/trabajador.dart';
import '../models/asignacion.dart';
import '../models/ejecucion_resultado.dart';

class ApiService {
  static final http.Client _client = http.Client();

  // 1. Login Operario
  static Future<Map<String, dynamic>> login(String username, String password) async {
    try {
      final response = await _client.post(
        Uri.parse(ApiConfig.loginUrl),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'username': username,
          'password': password,
        }),
      ).timeout(const Duration(seconds: 10));

      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return data;
    } catch (e) {
      return {
        'exitoso': false,
        'mensaje': 'Error de conexión con el servidor (${ApiConfig.baseUrl}): $e',
      };
    }
  }

  // 2. Obtener Asignación Activa del Operario con paradas ordenadas (1, 2, 3...)
  static Future<Asignacion?> obtenerAsignacionActiva(int idTrabajador) async {
    try {
      final url = ApiConfig.asignacionActivaUrl(idTrabajador);
      final response = await _client.get(
        Uri.parse(url),
        headers: {'Accept': 'application/json'},
      ).timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        if (data['tieneAsignacion'] == true && data['asignacion'] != null) {
          return Asignacion.fromJson(data['asignacion'] as Map<String, dynamic>);
        }
      }
      return null;
    } catch (e) {
      print('Error al obtener asignación activa: $e');
      return null;
    }
  }

  // 3. Subir fotografía del medidor al servidor
  static Future<String?> subirFotoMedidor(File fotoArchivo) async {
    try {
      final request = http.MultipartRequest('POST', Uri.parse(ApiConfig.subirFotoUrl));
      request.files.add(await http.MultipartFile.fromPath('foto', fotoArchivo.path));

      final streamedResponse = await request.send().timeout(const Duration(seconds: 15));
      final response = await http.Response.fromStream(streamedResponse);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        if (data['exito'] == true) {
          return data['url'] as String?;
        }
      }
      return null;
    } catch (e) {
      print('Error al subir foto: $e');
      return null;
    }
  }

  // 4. Registrar Ejecución en Campo (Foto, Lectura, Observación y validación GPS <= 60m)
  static Future<EjecucionResultado> registrarEjecucionCampo({
    required int idDetalle,
    required String estadoResultado, // 'Ejecutado', 'CasaCerrada', etc.
    required double? lecturaActual,
    required String? numeroMedidorReportado,
    required String? fotoMedidorUrl,
    required String? observacion,
    required double gpsLat,
    required double gpsLon,
    double gpsPrecisionM = 5.0,
    double toleranciaMetrosMax = 60.0,
  }) async {
    try {
      final payload = {
        'idDetalle': idDetalle,
        'estadoResultado': estadoResultado,
        'lecturaActual': lecturaActual,
        'numeroMedidorReportado': numeroMedidorReportado,
        'fotoMedidorUrl': fotoMedidorUrl,
        'observacion': observacion,
        'gpsLat': gpsLat,
        'gpsLon': gpsLon,
        'gpsPrecisionM': gpsPrecisionM,
        'toleranciaMetrosMax': toleranciaMetrosMax,
      };

      final response = await _client.post(
        Uri.parse(ApiConfig.ejecutarCampoUrl),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(payload),
      ).timeout(const Duration(seconds: 12));

      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return EjecucionResultado.fromJson(data);
    } catch (e) {
      return EjecucionResultado(
        exito: false,
        mensaje: 'Error de red al registrar corte/reconexión: $e',
        distanciaMetros: 999.0,
        validacionGpsExitosa: false,
        estadoFinal: 'Error',
      );
    }
  }

  // 5. Enviar Telemetría de Ubicación en Tiempo Real
  static Future<bool> enviarTelemetriaUbicacion({
    required int idTrabajador,
    required double latitud,
    required double longitud,
    double velocidadKmH = 0.0,
    double? rumbo,
    double precisionM = 5.0,
    int bateriaPorcentaje = 95,
  }) async {
    try {
      final response = await _client.post(
        Uri.parse(ApiConfig.telemetriaUbicacionUrl),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'idTrabajador': idTrabajador,
          'latitud': latitud,
          'longitud': longitud,
          'velocidadKmH': velocidadKmH,
          'rumbo': rumbo,
          'precisionM': precisionM,
          'bateriaPorcentaje': bateriaPorcentaje,
        }),
      ).timeout(const Duration(seconds: 8));

      return response.statusCode == 200;
    } catch (e) {
      // Telemetría silenciosa
      return false;
    }
  }

  // 6. Obtener lista de trabajadores disponibles
  static Future<List<Trabajador>> obtenerTrabajadores() async {
    try {
      final response = await _client.get(
        Uri.parse(ApiConfig.trabajadoresUrl),
      ).timeout(const Duration(seconds: 8));

      if (response.statusCode == 200) {
        final list = jsonDecode(response.body) as List<dynamic>;
        return list.map((item) => Trabajador.fromJson(item as Map<String, dynamic>)).toList();
      }
      return [];
    } catch (e) {
      return [];
    }
  }
}
