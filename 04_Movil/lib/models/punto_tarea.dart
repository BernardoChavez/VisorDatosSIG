class PuntoTarea {
  final int idDetalle;
  final int idAsignacion;
  final int idConexion;
  final String codigoConexion;
  final String numeroMedidor;
  final String? codigoLote;
  final String? codigoManzana;
  final String titular;
  final String direccion;
  final int ordenSecuencia;
  final double distanciaDesdeAnteriorM;
  String estado; // 'Pendiente', 'Ejecutado', 'NoRealizado'
  final double? lecturaAnterior;
  double? lecturaActual;
  String? numeroMedidorReportado;
  String? fotoMedidorUrl;
  String? observacion;
  DateTime? fechaHoraEjecucion;
  final double longitud;
  final double latitud;
  bool? validacionGpsExitosa;
  double? distanciaValidacionGpsM;

  PuntoTarea({
    required this.idDetalle,
    required this.idAsignacion,
    required this.idConexion,
    required this.codigoConexion,
    required this.numeroMedidor,
    this.codigoLote,
    this.codigoManzana,
    required this.titular,
    required this.direccion,
    required this.ordenSecuencia,
    required this.distanciaDesdeAnteriorM,
    required this.estado,
    this.lecturaAnterior,
    this.lecturaActual,
    this.numeroMedidorReportado,
    this.fotoMedidorUrl,
    this.observacion,
    this.fechaHoraEjecucion,
    required this.longitud,
    required this.latitud,
    this.validacionGpsExitosa,
    this.distanciaValidacionGpsM,
  });

  bool get estaCompletado => estado == 'Ejecutado' || estado == 'NoRealizado';
  bool get esExitoso => estado == 'Ejecutado';

  factory PuntoTarea.fromJson(Map<String, dynamic> json) {
    return PuntoTarea(
      idDetalle: json['idDetalle'] as int? ?? 0,
      idAsignacion: json['idAsignacion'] as int? ?? 0,
      idConexion: json['idConexion'] as int? ?? 0,
      codigoConexion: json['codigoConexion'] as String? ?? '',
      numeroMedidor: json['numeroMedidor'] as String? ?? '',
      codigoLote: json['codigoLote'] as String?,
      codigoManzana: json['codigoManzana'] as String?,
      titular: json['titular'] as String? ?? '',
      direccion: json['direccion'] as String? ?? '',
      ordenSecuencia: json['ordenSecuencia'] as int? ?? 1,
      distanciaDesdeAnteriorM: (json['distanciaDesdeAnteriorM'] is num)
          ? (json['distanciaDesdeAnteriorM'] as num).toDouble()
          : 0.0,
      estado: json['estado'] as String? ?? 'Pendiente',
      lecturaAnterior: (json['lecturaAnterior'] is num)
          ? (json['lecturaAnterior'] as num).toDouble()
          : null,
      lecturaActual: (json['lecturaActual'] is num)
          ? (json['lecturaActual'] as num).toDouble()
          : null,
      numeroMedidorReportado: json['numeroMedidorReportado'] as String?,
      fotoMedidorUrl: json['fotoMedidorUrl'] as String?,
      observacion: json['observacion'] as String?,
      fechaHoraEjecucion: json['fechaHoraEjecucion'] != null
          ? DateTime.tryParse(json['fechaHoraEjecucion'].toString())
          : null,
      longitud: (json['longitud'] is num)
          ? (json['longitud'] as num).toDouble()
          : 0.0,
      latitud: (json['latitud'] is num)
          ? (json['latitud'] as num).toDouble()
          : 0.0,
      validacionGpsExitosa: json['validacionGpsExitosa'] as bool?,
      distanciaValidacionGpsM: (json['distanciaValidacionGpsM'] is num)
          ? (json['distanciaValidacionGpsM'] as num).toDouble()
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'idDetalle': idDetalle,
      'idAsignacion': idAsignacion,
      'idConexion': idConexion,
      'codigoConexion': codigoConexion,
      'numeroMedidor': numeroMedidor,
      'codigoLote': codigoLote,
      'codigoManzana': codigoManzana,
      'titular': titular,
      'direccion': direccion,
      'ordenSecuencia': ordenSecuencia,
      'distanciaDesdeAnteriorM': distanciaDesdeAnteriorM,
      'estado': estado,
      'lecturaAnterior': lecturaAnterior,
      'lecturaActual': lecturaActual,
      'numeroMedidorReportado': numeroMedidorReportado,
      'fotoMedidorUrl': fotoMedidorUrl,
      'observacion': observacion,
      'fechaHoraEjecucion': fechaHoraEjecucion?.toIso8601String(),
      'longitud': longitud,
      'latitud': latitud,
      'validacionGpsExitosa': validacionGpsExitosa,
      'distanciaValidacionGpsM': distanciaValidacionGpsM,
    };
  }
}
