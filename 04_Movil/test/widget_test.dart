import 'package:flutter_test/flutter_test.dart';
import 'package:visor_datos_sig_movil/main.dart';
import 'package:visor_datos_sig_movil/models/trabajador.dart';
import 'package:visor_datos_sig_movil/models/punto_tarea.dart';
import 'package:visor_datos_sig_movil/models/asignacion.dart';
import 'package:visor_datos_sig_movil/services/location_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('Modelo Trabajador deserializa correctamente', () {
    final t = Trabajador.fromJson({
      'idTrabajador': 5,
      'codigoTrabajador': 'TRAB-001',
      'nombreCompleto': 'Juan Pérez Morales',
      'documentoIdentidad': '10234567',
      'tipoTrabajador': 'Cortador',
      'vehiculo': 'Moto',
      'estado': 'Disponible',
      'activo': true,
    });

    expect(t.idTrabajador, 5);
    expect(t.codigoTrabajador, 'TRAB-001');
    expect(t.tipoTrabajador, 'Cortador');
    expect(t.vehiculo, 'Moto');
  });

  test('Modelo Asignación ordena los puntos por secuencia Dijkstra (1, 2, 3...)', () {
    final asig = Asignacion.fromJson({
      'idAsignacion': 1,
      'codigoAsignacion': 'ASIG-001',
      'idTrabajador': 5,
      'nombreTrabajador': 'Juan Pérez',
      'tipoTrabajador': 'Cortador',
      'tipoServicio': 'Corte',
      'estado': 'Pendiente',
      'puntos': [
        {
          'idDetalle': 102,
          'idAsignacion': 1,
          'idConexion': 2,
          'codigoConexion': 'CON-002',
          'numeroMedidor': 'MED-002',
          'titular': 'Cliente B',
          'direccion': 'Calle 2',
          'ordenSecuencia': 2,
          'distanciaDesdeAnteriorM': 45.0,
          'estado': 'Pendiente',
          'longitud': -77.034,
          'latitud': -12.093,
        },
        {
          'idDetalle': 101,
          'idAsignacion': 1,
          'idConexion': 1,
          'codigoConexion': 'CON-001',
          'numeroMedidor': 'MED-001',
          'titular': 'Cliente A',
          'direccion': 'Calle 1',
          'ordenSecuencia': 1,
          'distanciaDesdeAnteriorM': 10.0,
          'estado': 'Pendiente',
          'longitud': -77.035,
          'latitud': -12.094,
        },
      ],
    });

    expect(asig.puntos.length, 2);
    // Verificar que se ordenaron 1, 2...
    expect(asig.puntos[0].ordenSecuencia, 1);
    expect(asig.puntos[0].codigoConexion, 'CON-001');
    expect(asig.puntos[1].ordenSecuencia, 2);
    expect(asig.puntos[1].codigoConexion, 'CON-002');
  });

  test('Cálculo de distancia geodésica geolocator es preciso', () {
    final dist = LocationService.calcularDistanciaMetros(
      latOrigen: -12.09350,
      lonOrigen: -77.03450,
      latDestino: -12.09350,
      lonDestino: -77.03450,
    );
    expect(dist, 0.0);
  });

  testWidgets('SigMovilApp renderiza pantalla de inicio y botón de ingreso', (WidgetTester tester) async {
    await tester.pumpWidget(const SigMovilApp());
    await tester.pumpAndSettle();

    expect(find.text('SIG - Agua Potable'), findsOneWidget);
    expect(find.text('Módulo Móvil de Cortes y Reconexiones'), findsOneWidget);
    expect(find.text('Ingresar a la Ruta'), findsOneWidget);
  });
}
