import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/trabajador.dart';
import 'api_service.dart';
import 'location_service.dart';

class AuthService {
  static const String keyTrabajador = 'sesion_trabajador_json';
  static const String keyToken = 'sesion_token';

  static Trabajador? _trabajadorActual;
  static String? _token;

  static Trabajador? get trabajadorActual => _trabajadorActual;
  static bool get estaAutenticado => _trabajadorActual != null;

  static Future<void> inicializar() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonStr = prefs.getString(keyTrabajador);
    _token = prefs.getString(keyToken);

    if (jsonStr != null && jsonStr.isNotEmpty) {
      try {
        final map = jsonDecode(jsonStr) as Map<String, dynamic>;
        _trabajadorActual = Trabajador.fromJson(map);
        // Reanudar telemetría si hay sesión activa
        LocationService.iniciarTelemetria(_trabajadorActual!.idTrabajador);
      } catch (e) {
        print('Error restaurando sesión: $e');
      }
    }
  }

  static Future<Map<String, dynamic>> iniciarSesion(String username, String password) async {
    final res = await ApiService.login(username, password);

    if (res['exitoso'] == true && res['trabajador'] != null) {
      final trabajadorData = res['trabajador'] as Map<String, dynamic>;
      _trabajadorActual = Trabajador.fromJson(trabajadorData);
      _token = res['token'] as String? ?? 'token_movil';

      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(keyTrabajador, jsonEncode(_trabajadorActual!.toJson()));
      await prefs.setString(keyToken, _token!);

      // Iniciar telemetría periódica GPS
      LocationService.iniciarTelemetria(_trabajadorActual!.idTrabajador);
    }

    return res;
  }

  static Future<void> cerrarSesion() async {
    LocationService.detenerTelemetria();
    _trabajadorActual = null;
    _token = null;

    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(keyTrabajador);
    await prefs.remove(keyToken);
  }
}
