import 'package:url_launcher/url_launcher.dart';

enum ModoNavegacion {
  moto,
  vehiculo,
  aPie,
}

class NavigationService {
  // Abrir navegación giro a giro en la app de mapas preferida (Google Maps / Waze / Apple Maps)
  static Future<bool> abrirNavegacionGiroAGiro({
    required double latitudDestino,
    required double longitudDestino,
    required String tituloPunto,
    ModoNavegacion modo = ModoNavegacion.moto,
  }) async {
    String googleModeParam = 'd'; // driving
    String travelModeParam = 'driving';

    switch (modo) {
      case ModoNavegacion.moto:
        googleModeParam = 'b'; // moto / dos ruedas o driving
        travelModeParam = 'driving';
        break;
      case ModoNavegacion.vehiculo:
        googleModeParam = 'd';
        travelModeParam = 'driving';
        break;
      case ModoNavegacion.aPie:
        googleModeParam = 'w';
        travelModeParam = 'walking';
        break;
    }

    // 1. Intent directo de Google Maps Navigation (giro a giro directo)
    final uriNavigation = Uri.parse(
      'google.navigation:q=$latitudDestino,$longitudDestino&mode=$googleModeParam',
    );

    // 2. URL estándar universal de Google Maps
    final uriGoogleMaps = Uri.parse(
      'https://www.google.com/maps/dir/?api=1&destination=$latitudDestino,$longitudDestino&travelmode=$travelModeParam',
    );

    // 3. URI estándar geo:
    final uriGeo = Uri.parse(
      'geo:$latitudDestino,$longitudDestino?q=$latitudDestino,$longitudDestino(${Uri.encodeComponent(tituloPunto)})',
    );

    try {
      if (await canLaunchUrl(uriNavigation)) {
        return await launchUrl(uriNavigation, mode: LaunchMode.externalApplication);
      } else if (await canLaunchUrl(uriGoogleMaps)) {
        return await launchUrl(uriGoogleMaps, mode: LaunchMode.externalApplication);
      } else if (await canLaunchUrl(uriGeo)) {
        return await launchUrl(uriGeo, mode: LaunchMode.externalApplication);
      }
    } catch (e) {
      print('Error al lanzar navegación giro a giro: $e');
    }

    // Fallback final en navegador web
    return await launchUrl(uriGoogleMaps, mode: LaunchMode.externalApplication);
  }
}
