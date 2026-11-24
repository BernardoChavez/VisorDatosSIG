class EjecucionResultado {
  final bool exito;
  final String mensaje;
  final double distanciaMetros;
  final bool validacionGpsExitosa;
  final String estadoFinal;

  EjecucionResultado({
    required this.exito,
    required this.mensaje,
    required this.distanciaMetros,
    required this.validacionGpsExitosa,
    required this.estadoFinal,
  });

  factory EjecucionResultado.fromJson(Map<String, dynamic> json) {
    return EjecucionResultado(
      exito: json['exito'] as bool? ?? false,
      mensaje: json['mensaje'] as String? ?? '',
      distanciaMetros: (json['distanciaMetros'] is num)
          ? (json['distanciaMetros'] as num).toDouble()
          : 0.0,
      validacionGpsExitosa: json['validacionGpsExitosa'] as bool? ?? false,
      estadoFinal: json['estadoFinal'] as String? ?? '',
    );
  }
}
