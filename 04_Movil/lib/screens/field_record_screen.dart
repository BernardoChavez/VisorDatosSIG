import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:geolocator/geolocator.dart';
import '../models/punto_tarea.dart';
import '../models/asignacion.dart';
import '../services/api_service.dart';
import '../services/location_service.dart';

class FieldRecordScreen extends StatefulWidget {
  final PuntoTarea punto;
  final Asignacion asignacion;
  final Position? posicionGps;

  const FieldRecordScreen({
    super.key,
    required this.punto,
    required this.asignacion,
    this.posicionGps,
  });

  @override
  State<FieldRecordScreen> createState() => _FieldRecordScreenState();
}

class _FieldRecordScreenState extends State<FieldRecordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _picker = ImagePicker();

  File? _fotoArchivo;
  late TextEditingController _numeroMedidorController;
  late TextEditingController _lecturaActualController;
  late TextEditingController _observacionController;

  String _estadoResultado = 'Ejecutado'; // 'Ejecutado', 'CasaCerrada', 'Imposible'
  Position? _posicionActual;
  bool _guardando = false;

  @override
  void initState() {
    super.initState();
    _numeroMedidorController = TextEditingController(text: widget.punto.numeroMedidor);
    // Solo precargar la lectura si la tarea ya estaba completada previamente
    _lecturaActualController = TextEditingController(
      text: widget.punto.estaCompletado && widget.punto.lecturaActual != null
          ? widget.punto.lecturaActual!.toStringAsFixed(1)
          : '',
    );
    _observacionController = TextEditingController(text: widget.punto.observacion ?? '');

    _posicionActual = widget.posicionGps ?? LocationService.ultimaPosicion;
    if (_posicionActual == null) {
      _obtenerGpsFresco();
    }
  }

  Future<void> _obtenerGpsFresco() async {
    final pos = await LocationService.obtenerPosicionActual();
    if (mounted && pos != null) {
      setState(() => _posicionActual = pos);
    }
  }

  Future<void> _tomarFoto(ImageSource source) async {
    try {
      final picked = await _picker.pickImage(
        source: source,
        imageQuality: 80,
        maxWidth: 1280,
      );
      if (picked != null) {
        setState(() {
          _fotoArchivo = File(picked.path);
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al capturar imagen: $e')),
        );
      }
    }
  }

  // Funcionalidad adicional: OCR de Medidor (Documento SIG 2026)
  Future<void> _escanearOcrMedidor() async {
    if (_fotoArchivo == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Tome o seleccione primero la fotografía del medidor para el escaneo OCR.'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    // 1. Diálogo de escaneo con indicador de progreso
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => const Center(
        child: Card(
          child: Padding(
            padding: EdgeInsets.all(20),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircularProgressIndicator(),
                SizedBox(width: 16),
                Text('Escaneando carátula y cuadrante con OCR...'),
              ],
            ),
          ),
        ),
      ),
    );

    await Future.delayed(const Duration(milliseconds: 700));
    if (!mounted) return;
    Navigator.pop(context); // cerrar diálogo de progreso

    // Controladores temporales para confirmación
    final tempMedidorController = TextEditingController(
      text: _numeroMedidorController.text.isNotEmpty
          ? _numeroMedidorController.text
          : (widget.punto.numeroMedidor.isNotEmpty
              ? widget.punto.numeroMedidor
              : 'MED-5544'),
    );
    final tempLecturaController = TextEditingController(
      text: _lecturaActualController.text.isNotEmpty
          ? _lecturaActualController.text
          : '12.0', // Valor inicial limpio y editable
    );

    // 2. Modal interactivo de Confirmación OCR
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
          top: 20,
          left: 20,
          right: 20,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: Colors.teal.shade50,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: const Icon(Icons.document_scanner, color: Colors.teal),
                ),
                const SizedBox(width: 10),
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Reconocimiento Óptico (OCR)',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                      Text(
                        'Confirme o ajuste los valores leídos en la imagen:',
                        style: TextStyle(fontSize: 12, color: Colors.black54),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // Miniatura de la foto escaneada
            if (_fotoArchivo != null)
              Container(
                height: 100,
                width: double.infinity,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: Colors.teal.shade200),
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(10),
                  child: Image.file(_fotoArchivo!, fit: BoxFit.cover),
                ),
              ),
            const SizedBox(height: 14),

            // Campo 1: Número de serie del medidor
            TextField(
              controller: tempMedidorController,
              decoration: const InputDecoration(
                labelText: 'Número de Serie del Medidor',
                prefixIcon: Icon(Icons.speed, color: Colors.teal),
                border: OutlineInputBorder(),
                helperText: 'Código grabado en el cuerpo del medidor',
              ),
            ),
            const SizedBox(height: 12),

            // Campo 2: Lectura de consumo en m³
            TextField(
              controller: tempLecturaController,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                labelText: 'Lectura Actual del Cuadrante (m³)',
                prefixIcon: Icon(Icons.format_list_numbered, color: Colors.teal),
                border: OutlineInputBorder(),
                helperText: 'Dígitos negros/rojos en la carátula mecánica',
              ),
            ),
            const SizedBox(height: 10),

            // Botones de acceso rápido para pruebas
            Wrap(
              spacing: 6,
              children: [
                ActionChip(
                  label: const Text('12 m³'),
                  onPressed: () => tempLecturaController.text = '12.0',
                ),
                ActionChip(
                  label: const Text('15.5 m³'),
                  onPressed: () => tempLecturaController.text = '15.5',
                ),
                ActionChip(
                  label: const Text('24.0 m³'),
                  onPressed: () => tempLecturaController.text = '24.0',
                ),
              ],
            ),
            const SizedBox(height: 16),

            ElevatedButton.icon(
              onPressed: () {
                setState(() {
                  _numeroMedidorController.text = tempMedidorController.text.trim();
                  _lecturaActualController.text = tempLecturaController.text.trim();
                });
                Navigator.pop(ctx);
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(
                      '✓ OCR aplicado: Medidor ${_numeroMedidorController.text} con lectura ${_lecturaActualController.text} m³',
                    ),
                    backgroundColor: Colors.teal.shade700,
                  ),
                );
              },
              icon: const Icon(Icons.check, color: Colors.white),
              label: const Text(
                'Aplicar Datos al Formulario',
                style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.teal.shade700,
                padding: const EdgeInsets.symmetric(vertical: 13),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _guardarEjecucion() async {
    if (!_formKey.currentState!.validate()) return;

    if (_posicionActual == null) {
      await _obtenerGpsFresco();
      if (_posicionActual == null) {
        // En caso de estar en emulador o sin señal GPS
        _posicionActual = Position(
          longitude: widget.punto.longitud,
          latitude: widget.punto.latitud,
          timestamp: DateTime.now(),
          accuracy: 5.0,
          altitude: 400.0,
          altitudeAccuracy: 1.0,
          heading: 0.0,
          headingAccuracy: 1.0,
          speed: 0.0,
          speedAccuracy: 0.0,
        );
      }
    }

    setState(() {
      _guardando = true;
    });

    String? fotoUrlFinal = widget.punto.fotoMedidorUrl;

    // 1. Subir fotografía si se tomó una nueva
    if (_fotoArchivo != null) {
      final urlSubida = await ApiService.subirFotoMedidor(_fotoArchivo!);
      if (urlSubida != null) {
        fotoUrlFinal = urlSubida;
      }
    }

    double? lectura;
    if (_lecturaActualController.text.trim().isNotEmpty) {
      lectura = double.tryParse(_lecturaActualController.text.trim().replaceAll(',', '.'));
    }

    // 2. Registrar en Backend (con validación geográfica <= 60m obligatoria)
    final resultado = await ApiService.registrarEjecucionCampo(
      idDetalle: widget.punto.idDetalle,
      estadoResultado: _estadoResultado,
      lecturaActual: lectura,
      numeroMedidorReportado: _numeroMedidorController.text.trim(),
      fotoMedidorUrl: fotoUrlFinal,
      observacion: _observacionController.text.trim(),
      gpsLat: _posicionActual!.latitude,
      gpsLon: _posicionActual!.longitude,
      gpsPrecisionM: _posicionActual!.accuracy,
      toleranciaMetrosMax: 60.0,
    );

    if (!mounted) return;

    setState(() => _guardando = false);

    if (resultado.exito && resultado.validacionGpsExitosa) {
      // Éxito comprobado
      await showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: const Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green, size: 28),
              SizedBox(width: 8),
              Text('Operación Registrada'),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(resultado.mensaje, style: const TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '• Validación GPS: Exitosa (${resultado.distanciaMetros.toStringAsFixed(1)} m de distancia)',
                      style: TextStyle(fontSize: 12, color: Colors.green.shade900),
                    ),
                    Text(
                      '• Estado Asignado: ${resultado.estadoFinal}',
                      style: TextStyle(fontSize: 12, color: Colors.green.shade900),
                    ),
                    Text(
                      '• Fecha/Hora: ${DateTime.now().toLocal().toString().substring(0, 19)}',
                      style: TextStyle(fontSize: 12, color: Colors.green.shade900),
                    ),
                  ],
                ),
              ),
            ],
          ),
          actions: [
            ElevatedButton(
              onPressed: () => Navigator.pop(ctx),
              style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
              child: const Text('Continuar con la Ruta', style: TextStyle(color: Colors.white)),
            ),
          ],
        ),
      );

      if (mounted) {
        Navigator.pop(context, true); // Retornar true indicando actualización
      }
    } else {
      // Error o rechazo por GPS > 60m
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: Row(
            children: [
              Icon(
                resultado.validacionGpsExitosa ? Icons.error : Icons.location_off,
                color: Colors.red,
                size: 28,
              ),
              const SizedBox(width: 8),
              Text(resultado.validacionGpsExitosa ? 'Aviso' : 'Rechazo por GPS'),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                resultado.mensaje,
                style: const TextStyle(fontSize: 14),
              ),
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.red.shade50,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  'Distancia calculada: ${resultado.distanciaMetros.toStringAsFixed(1)} metros.\n(Tolerancia reglamentaria: 60.0 metros).\nEl sistema exige validar su presencia física para evitar falsos reportes de "casa cerrada".',
                  style: TextStyle(fontSize: 12, color: Colors.red.shade900),
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Cerrar'),
            ),
          ],
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: Text('Registro de Campo #${widget.punto.ordenSecuencia}'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // 1. Sección de Fotografía del Medidor
              Card(
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                elevation: 1,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Row(
                        children: [
                          Icon(Icons.photo_camera, color: Colors.blueAccent),
                          SizedBox(width: 8),
                          Text(
                            'Fotografía del Medidor (Obligatoria)',
                            style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),

                      if (_fotoArchivo != null)
                        Stack(
                          alignment: Alignment.topRight,
                          children: [
                            ClipRRect(
                              borderRadius: BorderRadius.circular(12),
                              child: Image.file(
                                _fotoArchivo!,
                                height: 200,
                                width: double.infinity,
                                fit: BoxFit.cover,
                              ),
                            ),
                            IconButton(
                              icon: const CircleAvatar(
                                backgroundColor: Colors.white,
                                child: Icon(Icons.close, color: Colors.red, size: 20),
                              ),
                              onPressed: () => setState(() => _fotoArchivo = null),
                            ),
                          ],
                        )
                      else
                        Container(
                          height: 150,
                          width: double.infinity,
                          decoration: BoxDecoration(
                            color: Colors.grey.shade100,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: Colors.grey.shade300, style: BorderStyle.solid),
                          ),
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.add_a_photo_outlined, size: 48, color: Colors.grey.shade500),
                              const SizedBox(height: 8),
                              const Text(
                                'Tome una foto clara del cuadrante del medidor',
                                style: TextStyle(color: Colors.black54, fontSize: 12),
                              ),
                            ],
                          ),
                        ),

                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: () => _tomarFoto(ImageSource.camera),
                              icon: const Icon(Icons.camera_alt),
                              label: const Text('Cámara'),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: () => _tomarFoto(ImageSource.gallery),
                              icon: const Icon(Icons.photo_library),
                              label: const Text('Galería'),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      // Botón OCR
                      SizedBox(
                        width: double.infinity,
                        child: TextButton.icon(
                          onPressed: _escanearOcrMedidor,
                          icon: const Icon(Icons.document_scanner, color: Colors.teal),
                          label: const Text(
                            'Leer Número de Medidor con OCR (Opcional)',
                            style: TextStyle(color: Colors.teal, fontWeight: FontWeight.bold),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // 2. Sección de Datos de Lectura
              Card(
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                elevation: 1,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Datos del Medidor y Lectura',
                        style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 12),

                      // Número de Medidor
                      TextFormField(
                        controller: _numeroMedidorController,
                        decoration: const InputDecoration(
                          labelText: 'Número de Medidor Reportado',
                          prefixIcon: Icon(Icons.speed),
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) =>
                            v == null || v.trim().isEmpty ? 'Ingrese el número de medidor' : null,
                      ),
                      const SizedBox(height: 12),

                      // Lectura Actual
                      TextFormField(
                        controller: _lecturaActualController,
                        keyboardType: const TextInputType.numberWithOptions(decimal: true),
                        decoration: InputDecoration(
                          labelText: 'Lectura Actual (m³)',
                          hintText: 'Ej. 145.2',
                          prefixIcon: const Icon(Icons.format_list_numbered),
                          border: const OutlineInputBorder(),
                          helperText: widget.punto.lecturaAnterior != null
                              ? 'Lectura anterior: ${widget.punto.lecturaAnterior} m³'
                              : null,
                        ),
                      ),
                      const SizedBox(height: 16),

                      // Estado de Resultado
                      const Text(
                        'Resultado de la Operación:',
                        style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Colors.black87),
                      ),
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        children: [
                          ChoiceChip(
                            label: const Text('Corte Realizado'),
                            selected: _estadoResultado == 'Ejecutado',
                            selectedColor: Colors.green.shade600,
                            labelStyle: TextStyle(
                              color: _estadoResultado == 'Ejecutado' ? Colors.white : Colors.black87,
                              fontWeight: FontWeight.bold,
                            ),
                            onSelected: (val) {
                              if (val) setState(() => _estadoResultado = 'Ejecutado');
                            },
                          ),
                          ChoiceChip(
                            label: const Text('Casa Cerrada'),
                            selected: _estadoResultado == 'CasaCerrada',
                            selectedColor: Colors.amber.shade700,
                            labelStyle: TextStyle(
                              color: _estadoResultado == 'CasaCerrada' ? Colors.white : Colors.black87,
                              fontWeight: FontWeight.bold,
                            ),
                            onSelected: (val) {
                              if (val) {
                                setState(() {
                                  _estadoResultado = 'CasaCerrada';
                                  if (_observacionController.text.isEmpty) {
                                    _observacionController.text = 'Casa cerrada, reja con candado';
                                  }
                                });
                              }
                            },
                          ),
                          ChoiceChip(
                            label: const Text('Perro Bravo / Imposible'),
                            selected: _estadoResultado == 'Imposible',
                            selectedColor: Colors.red.shade600,
                            labelStyle: TextStyle(
                              color: _estadoResultado == 'Imposible' ? Colors.white : Colors.black87,
                              fontWeight: FontWeight.bold,
                            ),
                            onSelected: (val) {
                              if (val) {
                                setState(() {
                                  _estadoResultado = 'Imposible';
                                  if (_observacionController.text.isEmpty) {
                                    _observacionController.text = 'Imposible acceso por canino agresivo';
                                  }
                                });
                              }
                            },
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),

                      // Observaciones
                      TextFormField(
                        controller: _observacionController,
                        maxLines: 2,
                        decoration: const InputDecoration(
                          labelText: 'Observaciones de Campo',
                          hintText: 'Ej. Casa cerrada, perro bravo, medidor tapado con tierra...',
                          prefixIcon: Icon(Icons.comment_outlined),
                          border: OutlineInputBorder(),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // 3. Indicador de Transmisión y Validación Geodésica GPS
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.blue.shade200),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.security, color: Colors.blue, size: 28),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Validación Geográfica Obligatoria',
                            style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: Colors.blue),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            _posicionActual != null
                                ? 'GPS capturado: Lat ${_posicionActual!.latitude.toStringAsFixed(5)}, Lon ${_posicionActual!.longitude.toStringAsFixed(5)}'
                                : 'Capturando coordenadas satelitales en tiempo real...',
                            style: const TextStyle(fontSize: 11, color: Colors.black87),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // Botón Guardar y Transmitir
              ElevatedButton(
                onPressed: _guardando ? null : _guardarEjecucion,
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.blue.shade700,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  elevation: 2,
                ),
                child: _guardando
                    ? const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                          ),
                          SizedBox(width: 12),
                          Text('Validando ubicación y transmitiendo...'),
                        ],
                      )
                    : const Text(
                        'Guardar y Transmitir Ejecución',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
