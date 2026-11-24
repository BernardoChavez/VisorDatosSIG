import 'package:flutter/material.dart';
import '../config/api_config.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../models/trabajador.dart';
import 'task_list_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController(text: 'TRAB-001');
  final _passwordController = TextEditingController(text: '123456');
  bool _obscurePassword = true;
  bool _cargando = false;
  String? _mensajeError;

  List<Trabajador> _trabajadoresDisponibles = [];

  @override
  void initState() {
    super.initState();
    _cargarTrabajadoresSugeridos();
  }

  Future<void> _cargarTrabajadoresSugeridos() async {
    final lista = await ApiService.obtenerTrabajadores();
    if (mounted) {
      setState(() {
        _trabajadoresDisponibles = lista;
      });
    }
  }

  Future<void> _ejecutarLogin() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _cargando = true;
      _mensajeError = null;
    });

    final res = await AuthService.iniciarSesion(
      _usernameController.text.trim(),
      _passwordController.text.trim(),
    );

    if (!mounted) return;

    setState(() => _cargando = false);

    if (res['exitoso'] == true) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const TaskListScreen()),
      );
    } else {
      setState(() {
        _mensajeError = res['mensaje'] as String? ?? 'Error de autenticación.';
      });
    }
  }

  void _abrirAjustesServidor() {
    final serverController = TextEditingController(text: ApiConfig.baseUrl);
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.settings_ethernet, color: Colors.blueAccent),
            SizedBox(width: 8),
            Text('IP del Servidor SIG'),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Ingrese la IP y puerto del servidor local en su red Wi-Fi para pruebas:',
              style: TextStyle(fontSize: 13, color: Colors.black54),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: serverController,
              decoration: const InputDecoration(
                labelText: 'URL Base',
                hintText: 'http://192.168.0.9:5000',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.wifi),
              ),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 6,
              children: [
                ActionChip(
                  label: const Text('192.168.0.9'),
                  onPressed: () => serverController.text = 'http://192.168.0.9:5000',
                ),
                ActionChip(
                  label: const Text('10.0.2.2 (Emulador)'),
                  onPressed: () => serverController.text = 'http://10.0.2.2:5000',
                ),
                ActionChip(
                  label: const Text('localhost'),
                  onPressed: () => serverController.text = 'http://localhost:5000',
                ),
              ],
            )
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancelar'),
          ),
          ElevatedButton(
            onPressed: () async {
              await ApiConfig.setBaseUrl(serverController.text);
              if (!ctx.mounted) return;
              Navigator.pop(ctx);
              setState(() {});
              _cargarTrabajadoresSugeridos();
              if (mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text('Servidor configurado en: ${ApiConfig.baseUrl}')),
                );
              }
            },
            child: const Text('Guardar'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF4F7FB),
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.settings, color: Colors.blueGrey),
            tooltip: 'Configurar IP Servidor',
            onPressed: _abrirAjustesServidor,
          ),
        ],
      ),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 16.0),
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Logo / Encabezado
                  Center(
                    child: Container(
                      width: 86,
                      height: 86,
                      decoration: BoxDecoration(
                        color: Colors.blue.shade700,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: Colors.blue.withOpacity(0.3),
                            blurRadius: 16,
                            offset: const Offset(0, 6),
                          ),
                        ],
                      ),
                      child: const Icon(
                        Icons.water_drop_rounded,
                        size: 48,
                        color: Colors.white,
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'SIG - Agua Potable',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF1E293B),
                    ),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Módulo Móvil de Cortes y Reconexiones',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 14,
                      color: Color(0xFF64748B),
                    ),
                  ),
                  const SizedBox(height: 8),
                  // Indicador de Servidor
                  Center(
                    child: InkWell(
                      onTap: _abrirAjustesServidor,
                      borderRadius: BorderRadius.circular(16),
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.blue.shade50,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: Colors.blue.shade200),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.wifi, size: 14, color: Colors.blue),
                            const SizedBox(width: 6),
                            Text(
                              ApiConfig.baseUrl,
                              style: TextStyle(fontSize: 11, color: Colors.blue.shade800),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),

                  if (_mensajeError != null)
                    Container(
                      margin: const EdgeInsets.only(bottom: 16),
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: Colors.red.shade50,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.red.shade200),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.error_outline, color: Colors.red.shade700, size: 20),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              _mensajeError!,
                              style: TextStyle(fontSize: 13, color: Colors.red.shade800),
                            ),
                          ),
                        ],
                      ),
                    ),

                  // Selector rápido de Operario (para pruebas rápidas en campo)
                  if (_trabajadoresDisponibles.isNotEmpty) ...[
                    const Text(
                      'Operarios Disponibles (Toque para seleccionar):',
                      style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.black54),
                    ),
                    const SizedBox(height: 6),
                    SizedBox(
                      height: 52,
                      child: ListView.separated(
                        scrollDirection: Axis.horizontal,
                        itemCount: _trabajadoresDisponibles.length,
                        separatorBuilder: (_, __) => const SizedBox(width: 8),
                        itemBuilder: (ctx, i) {
                          final t = _trabajadoresDisponibles[i];
                          final esSeleccionado = _usernameController.text == t.codigoTrabajador;
                          return ChoiceChip(
                            label: Row(
                              children: [
                                Icon(
                                  t.tipoTrabajador == 'Cortador'
                                      ? Icons.content_cut
                                      : Icons.build_circle,
                                  size: 14,
                                  color: esSeleccionado ? Colors.white : Colors.blueGrey,
                                ),
                                const SizedBox(width: 6),
                                Text('${t.codigoTrabajador} (${t.nombreCompleto.split(' ').first})'),
                              ],
                            ),
                            selected: esSeleccionado,
                            selectedColor: Colors.blue.shade700,
                            labelStyle: TextStyle(
                              color: esSeleccionado ? Colors.white : Colors.black87,
                              fontSize: 12,
                            ),
                            onSelected: (val) {
                              if (val) {
                                setState(() {
                                  _usernameController.text = t.codigoTrabajador;
                                });
                              }
                            },
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // Campo Usuario / Código de Operario
                  TextFormField(
                    controller: _usernameController,
                    decoration: InputDecoration(
                      labelText: 'Usuario o Código de Operario',
                      hintText: 'Ej. TRAB-001 o admin',
                      prefixIcon: const Icon(Icons.badge_outlined),
                      filled: true,
                      fillColor: Colors.white,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: BorderSide(color: Colors.grey.shade300),
                      ),
                    ),
                    validator: (v) =>
                        v == null || v.trim().isEmpty ? 'Ingrese su usuario o código' : null,
                  ),
                  const SizedBox(height: 16),

                  // Campo Contraseña
                  TextFormField(
                    controller: _passwordController,
                    obscureText: _obscurePassword,
                    decoration: InputDecoration(
                      labelText: 'Contraseña',
                      prefixIcon: const Icon(Icons.lock_outline),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword ? Icons.visibility_off : Icons.visibility,
                          color: Colors.grey,
                        ),
                        onPressed: () =>
                            setState(() => _obscurePassword = !_obscurePassword),
                      ),
                      filled: true,
                      fillColor: Colors.white,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: BorderSide(color: Colors.grey.shade300),
                      ),
                    ),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'Ingrese su contraseña' : null,
                  ),
                  const SizedBox(height: 24),

                  // Botón Iniciar Sesión
                  ElevatedButton(
                    onPressed: _cargando ? null : _ejecutarLogin,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.blue.shade700,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      elevation: 2,
                    ),
                    child: _cargando
                        ? const SizedBox(
                            height: 22,
                            width: 22,
                            child: CircularProgressIndicator(
                              color: Colors.white,
                              strokeWidth: 2.5,
                            ),
                          )
                        : const Text(
                            'Ingresar a la Ruta',
                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
