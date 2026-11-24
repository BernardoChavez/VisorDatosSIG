import 'punto_tarea.dart';

class Asignacion {
  final int idAsignacion;
  final String codigoAsignacion;
  final int idTrabajador;
  final String nombreTrabajador;
  final String tipoTrabajador;
  final DateTime fechaAsignacion;
  final String tipoServicio; // 'Corte' o 'Reconexion'
  final String estado; // 'Pendiente', 'EnCurso', 'Finalizado'
  final int cantidadPuntos;
  final int puntosCompletados;
  final double distanciaTotalMetros;
  final String? rutaOptimaWkt;
  final String puntoPartidaNombre;
  final double puntoPartidaLon;
  final double puntoPartidaLat;
  final List<PuntoTarea> puntos;

  Asignacion({
    required this.idAsignacion,
    required this.codigoAsignacion,
    required this.idTrabajador,
    required this.nombreTrabajador,
    required this.tipoTrabajador,
    required this.fechaAsignacion,
    required this.tipoServicio,
    required this.estado,
    required this.cantidadPuntos,
    required this.puntosCompletados,
    required this.distanciaTotalMetros,
    this.rutaOptimaWkt,
    required this.puntoPartidaNombre,
    required this.puntoPartidaLon,
    required this.puntoPartidaLat,
    required this.puntos,
  });

  int get puntosPendientes => puntos.where((p) => !p.estaCompletado).length;
  double get porcentajeAvance =>
      cantidadPuntos > 0 ? (puntosCompletados / cantidadPuntos) * 100.0 : 0.0;

  factory Asignacion.fromJson(Map<String, dynamic> json) {
    var rawPuntos = json['puntos'] as List<dynamic>? ?? [];
    List<PuntoTarea> listaPuntos =
        rawPuntos.map((p) => PuntoTarea.fromJson(p as Map<String, dynamic>)).toList();

    // Ordenar explícitamente por ordenSecuencia (1, 2, 3...)
    listaPuntos.sort((a, b) => a.ordenSecuencia.compareTo(b.ordenSecuencia));

    return Asignacion(
      idAsignacion: json['idAsignacion'] as int? ?? 0,
      codigoAsignacion: json['codigoAsignacion'] as String? ?? '',
      idTrabajador: json['idTrabajador'] as int? ?? 0,
      nombreTrabajador: json['nombreTrabajador'] as String? ?? '',
      tipoTrabajador: json['tipoTrabajador'] as String? ?? 'Cortador',
      fechaAsignacion: json['fechaAsignacion'] != null
          ? DateTime.tryParse(json['fechaAsignacion'].toString()) ?? DateTime.now()
          : DateTime.now(),
      tipoServicio: json['tipoServicio'] as String? ?? 'Corte',
      estado: json['estado'] as String? ?? 'Pendiente',
      cantidadPuntos: json['cantidadPuntos'] as int? ?? listaPuntos.length,
      puntosCompletados: json['puntosCompletados'] as int? ??
          listaPuntos.where((p) => p.estaCompletado).length,
      distanciaTotalMetros: (json['distanciaTotalMetros'] is num)
          ? (json['distanciaTotalMetros'] as num).toDouble()
          : 0.0,
      rutaOptimaWkt: json['rutaOptimaWkt'] as String?,
      puntoPartidaNombre:
          json['puntoPartidaNombre'] as String? ?? 'Almacén Central / Oficina',
      puntoPartidaLon: (json['puntoPartidaLon'] is num)
          ? (json['puntoPartidaLon'] as num).toDouble()
          : -77.03450,
      puntoPartidaLat: (json['puntoPartidaLat'] is num)
          ? (json['puntoPartidaLat'] as num).toDouble()
          : -12.09350,
      puntos: listaPuntos,
    );
  }
}
