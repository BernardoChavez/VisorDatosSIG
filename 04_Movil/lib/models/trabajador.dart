class Trabajador {
  final int idTrabajador;
  final String codigoTrabajador;
  final String nombreCompleto;
  final String documentoIdentidad;
  final String? telefono;
  final String tipoTrabajador; // 'Cortador' o 'Reconector'
  final String? vehiculo;
  final String estado;
  final bool activo;

  Trabajador({
    required this.idTrabajador,
    required this.codigoTrabajador,
    required this.nombreCompleto,
    required this.documentoIdentidad,
    this.telefono,
    required this.tipoTrabajador,
    this.vehiculo,
    required this.estado,
    required this.activo,
  });

  factory Trabajador.fromJson(Map<String, dynamic> json) {
    return Trabajador(
      idTrabajador: json['idTrabajador'] as int? ?? 0,
      codigoTrabajador: json['codigoTrabajador'] as String? ?? '',
      nombreCompleto: json['nombreCompleto'] as String? ?? '',
      documentoIdentidad: json['documentoIdentidad'] as String? ?? '',
      telefono: json['telefono'] as String?,
      tipoTrabajador: json['tipoTrabajador'] as String? ?? 'Cortador',
      vehiculo: json['vehiculo'] as String?,
      estado: json['estado'] as String? ?? 'Disponible',
      activo: json['activo'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'idTrabajador': idTrabajador,
      'codigoTrabajador': codigoTrabajador,
      'nombreCompleto': nombreCompleto,
      'documentoIdentidad': documentoIdentidad,
      'telefono': telefono,
      'tipoTrabajador': tipoTrabajador,
      'vehiculo': vehiculo,
      'estado': estado,
      'activo': activo,
    };
  }
}
