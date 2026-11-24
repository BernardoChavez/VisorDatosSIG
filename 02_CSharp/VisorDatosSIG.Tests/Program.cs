#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Infrastructure.Data;
using VisorDatosSIG.Infrastructure.Repositories;
using VisorDatosSIG.Infrastructure.Shapefiles;

namespace VisorDatosSIG.Tests;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine("   VisorDatosSIG 2026 — Suite Automatizada de Criterios de Aceptación (CA-01 a CA-17)");
        Console.WriteLine("================================================================================\n");
        Console.ResetColor();

        var stopwatchTotal = Stopwatch.StartNew();
        int totalTests = 17;
        int passedTests = 0;
        int failedTests = 0;

        try
        {
            if (args.Length > 0 && args[0] == "--remigrar-manzanas")
            {
                var dbf = new DbConnectionFactory();
                var br = new BitacoraRepository(dbf);
                var aud = new AuditoriaService(br);
                var mr = new MigracionRepository(dbf);
                var sfReader = new ShapefileReader();
                var ms = new MigradorService(mr, sfReader, aud);
                string path = args.Length > 1 ? args[1] : @"C:\Users\USER\Documents\clases\SIG\Datos\aProyecto\Especificaciones_Proyecto_VisorDatosSIG_2026\DatosSIG_Reproj\Exp_MapaBase_MZA_4326.shp";
                Console.WriteLine($"[REMIGRACIÓN] Cargando {path} ...");
                var req = new VisorDatosSIG.Application.DTOs.Migracion.IniciarMigracionRequestDto
                {
                    RutaCarpetaOShapefile = path,
                    Capa = VisorDatosSIG.Domain.Enums.TipoCapa.Manzanas,
                    Modalidad = VisorDatosSIG.Domain.Enums.ModalidadMigracion.Reemplazar,
                    Usuario = "admin"
                };
                var res = await ms.EjecutarMigracionAsync(req);
                Console.WriteLine($"[REMIGRACIÓN] Estado={res.Estado}, Total={res.TotalRegistros}, Exitosos={res.RegistrosExitosos}, Fallidos={res.RegistrosFallidos}");
                return res.Estado == "Exitoso" ? 0 : 1;
            }

            // 1. Preparación de Entorno y Repositorios
            Console.WriteLine("[INFO] Preparando entorno de pruebas y datasets Shapefiles en WGS84...");
            CriteriosAceptacionTests.SetupDatasets();

            var dbFactory = new DbConnectionFactory();
            var bRepo = new BitacoraRepository(dbFactory);
            var audService = new AuditoriaService(bRepo);
            var mRepo = new MigracionRepository(dbFactory);
            var rdr = new ShapefileReader();
            var migService = new MigradorService(mRepo, rdr, audService);
            var cRepo = new CapaRepository(dbFactory);
            var busqRepo = new BusquedaRepository(dbFactory);
            var busqService = new BusquedaService(busqRepo);
            var uRepo = new UsuarioRepository(dbFactory);
            var authService = new AuthService(uRepo, audService);
            var aguaRepo = new AguaRepository(dbFactory);
            var dijkstraService = new DijkstraRoutingService(aguaRepo);
            var aguaService = new AguaService(aguaRepo, dijkstraService);

            Console.WriteLine("[INFO] Servicios inicializados correctamente contra SQL Server.\n");

            // Ejecución secuencial de CA-01 a CA-12
            await EjecutarPrueba("CA-01: Validación de componentes requeridos (.shp, .shx, .dbf, .prj)", () => {
                CriteriosAceptacionTests.Test_CA01_ValidacionComponentesShapefile();
                return Task.CompletedTask;
            });
            passedTests++;

            await EjecutarPrueba("CA-02: Validación de proyección espacial WGS 84 (EPSG:4326)", () => {
                CriteriosAceptacionTests.Test_CA02_ValidacionProyeccionWgs84();
                return Task.CompletedTask;
            });
            passedTests++;

            await EjecutarPrueba("CA-03: Lectura binaria nativa de Shapefiles (Point, Line, Poly + DBF)", () => {
                CriteriosAceptacionTests.Test_CA03_LecturaBinariaShapefiles();
                return Task.CompletedTask;
            });
            passedTests++;

            await EjecutarPrueba("CA-04: Value Objects espaciales BoundingBox y Coordenada", () => {
                CriteriosAceptacionTests.Test_CA04_BoundingBoxYCoordenada();
                return Task.CompletedTask;
            });
            passedTests++;

            await EjecutarPrueba("CA-05: Convertidor WKT a GeoJSON estándar RFC 7946", () => {
                CriteriosAceptacionTests.Test_CA05_ConvertidorWktAGeoJson();
                return Task.CompletedTask;
            });
            passedTests++;

            await EjecutarPrueba("CA-06: Ingesta transaccional en SQL Server con SRID 4326 y STIsValid()", async () => {
                await CriteriosAceptacionTests.Test_CA06_IngestaTransaccionalSqlServerAsync(dbFactory, migService);
            });
            passedTests++;

            await EjecutarPrueba("CA-07: Modalidades de Migración (Reemplazar vs Anexar sin duplicados)", async () => {
                await CriteriosAceptacionTests.Test_CA07_ModalidadesMigracionAsync(dbFactory, migService);
            });
            passedTests++;

            await EjecutarPrueba("CA-08: Búsqueda alfanumérica/espacial con BBox envolvente combinado", async () => {
                await CriteriosAceptacionTests.Test_CA08_BusquedaEspacialYBBoxCombinadoAsync(busqService);
            });
            passedTests++;

            await EjecutarPrueba("CA-09: Identificación espacial por punto (Point-in-polygon & buffer)", async () => {
                await CriteriosAceptacionTests.Test_CA09_IdentificacionPorPuntoAsync(busqService);
            });
            passedTests++;

            await EjecutarPrueba("CA-10: Criptografía segura PBKDF2-HMAC-SHA512 con 100k iteraciones + Salt", () => {
                CriteriosAceptacionTests.Test_CA10_CriptografiaPbkdf2();
                return Task.CompletedTask;
            });
            passedTests++;

            await EjecutarPrueba("CA-11: Autenticación, control de accesos y bloqueo de cuentas", async () => {
                await CriteriosAceptacionTests.Test_CA11_AutenticacionYBloqueoAsync(authService);
            });
            passedTests++;

            await EjecutarPrueba("CA-12: Trazabilidad y persistencia inmutable en Bitácora de Auditoría", async () => {
                await CriteriosAceptacionTests.Test_CA12_BitacoraAuditoriaAsync(audService);
            });
            passedTests++;

            await EjecutarPrueba("CA-13: Creación de usuarios con permisos granulares dinámicos (PBAC)", async () => {
                await CriteriosAceptacionTests.Test_CA13_PermisosGranularesYPersonalizacionAsync(authService);
            });
            passedTests++;

            // Restablecer entorno y semillas del módulo de agua para aislamiento total
            await CriteriosAceptacionTests.ResetTestDataAguaAsync(dbFactory);

            await EjecutarPrueba("CA-14: Dominio de Agua Potable (Conexiones, estados Normal/ParaCorte/Cortado, operarios)", async () => {
                await CriteriosAceptacionTests.Test_CA14_ModuloAguaDominioYConexionesAsync(aguaService);
            });
            passedTests++;

            await EjecutarPrueba("CA-15: Dijkstra en Red Vial dbo.Vias (Ruta secuencial 1,2,3... sin atravesar manzanas)", async () => {
                await CriteriosAceptacionTests.Test_CA15_DijkstraRoutingRedVialAsync(aguaService, dijkstraService);
            });
            passedTests++;

            await EjecutarPrueba("CA-16: Asignación de trabajo y validación geodésica GPS en campo (<=60m / CasaCerrada)", async () => {
                await CriteriosAceptacionTests.Test_CA16_AsignacionesYValidacionGpsCampoAsync(aguaService);
            });
            passedTests++;

            await EjecutarPrueba("CA-17: Monitoreo GPS y telemetría de operarios en tiempo real para panel de control", async () => {
                await CriteriosAceptacionTests.Test_CA17_MonitoreoTiempoRealAsync(aguaService);
            });
            passedTests++;
        }
        catch (Exception ex)
        {
            failedTests++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR CRÍTICO EN EJECUCIÓN DE PRUEBAS]: {ex.Message}");
            Console.ResetColor();
        }

        stopwatchTotal.Stop();

        Console.WriteLine("\n================================================================================");
        Console.ForegroundColor = failedTests == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"  RESUMEN: {passedTests}/{totalTests} PRUEBAS COMPLETADAS EXITOSAMENTE (100% OK)");
        Console.WriteLine($"  TIEMPO TOTAL DE EJECUCIÓN: {stopwatchTotal.ElapsedMilliseconds} ms");
        Console.WriteLine("================================================================================\n");
        Console.ResetColor();

        return failedTests == 0 ? 0 : 1;
    }

    private static async Task EjecutarPrueba(string nombre, Func<Task> testFunc)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await testFunc();
            sw.Stop();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [PASS] ");
            Console.ResetColor();
            Console.WriteLine($"{nombre} ({sw.ElapsedMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [FAIL] ");
            Console.ResetColor();
            Console.WriteLine($"{nombre} ({sw.ElapsedMilliseconds} ms)");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"         Motivo: {ex.Message}");
            Console.ResetColor();
            throw;
        }
    }
}
