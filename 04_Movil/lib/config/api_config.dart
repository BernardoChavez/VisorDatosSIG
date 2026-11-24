import 'package:shared_preferences/shared_preferences.dart';

class ApiConfig {
  static const String keyServerUrl = 'custom_server_base_url';
  
  // IP Wi-Fi predeterminada del servidor host en la red local
  static const String defaultBaseUrl = 'http://192.168.0.9:5000';

  static String _activeBaseUrl = defaultBaseUrl;

  static String get baseUrl => _activeBaseUrl;

  static Future<void> inicializar() async {
    final prefs = await SharedPreferences.getInstance();
    final saved = prefs.getString(keyServerUrl);
    if (saved != null && saved.trim().isNotEmpty) {
      _activeBaseUrl = saved.trim();
    } else {
      _activeBaseUrl = defaultBaseUrl;
    }
  }

  static Future<void> setBaseUrl(String newUrl) async {
    String cleanUrl = newUrl.trim();
    if (cleanUrl.endsWith('/')) {
      cleanUrl = cleanUrl.substring(0, cleanUrl.length - 1);
    }
    if (!cleanUrl.startsWith('http://') && !cleanUrl.startsWith('https://')) {
      cleanUrl = 'http://$cleanUrl';
    }
    _activeBaseUrl = cleanUrl;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(keyServerUrl, _activeBaseUrl);
  }

  // Endpoints
  static String get loginUrl => '$_activeBaseUrl/api/agua/mobile/login';
  static String asignacionActivaUrl(int idTrabajador) =>
      '$_activeBaseUrl/api/agua/mobile/asignacion-activa/$idTrabajador';
  static String get ejecutarCampoUrl => '$_activeBaseUrl/api/agua/campo/ejecutar';
  static String get telemetriaUbicacionUrl => '$_activeBaseUrl/api/agua/monitoreo/ubicacion';
  static String get subirFotoUrl => '$_activeBaseUrl/api/agua/mobile/subir-foto';
  static String get trabajadoresUrl => '$_activeBaseUrl/api/agua/trabajadores';
  static String get dashboardUrl => '$_activeBaseUrl/api/agua/dashboard';

  static String resolverUrlFoto(String? fotoUrl) {
    if (fotoUrl == null || fotoUrl.isEmpty) return '';
    if (fotoUrl.startsWith('http://') || fotoUrl.startsWith('https://')) {
      return fotoUrl;
    }
    if (fotoUrl.startsWith('/')) {
      return '$_activeBaseUrl$fotoUrl';
    }
    return '$_activeBaseUrl/$fotoUrl';
  }
}
