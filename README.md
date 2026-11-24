# 🌐 VisorDatosSIG 2026
### Sistema Corporativo de Información Geográfica, Catastro Territorial y Gestión de Cortes y Reconexiones de Agua Potable

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022%20Spatial-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![Leaflet](https://img.shields.io/badge/Leaflet-1.9.4-199900?logo=leaflet&logoColor=white)](https://leafletjs.com/)
[![Flutter](https://img.shields.io/badge/Flutter-3.x-02569B?logo=flutter&logoColor=white)](https://flutter.dev/)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![EPSG:4326](https://img.shields.io/badge/CRS-EPSG%3A4326%20WGS84-blue)](#)

---

## 🏛️ Contexto Académico e Institucional

* **Institución:** Universidad Autónoma Gabriel René Moreno (UAGRM)
* **Facultad:** Facultad de Ingeniería en Ciencias de la Computación y Telecomunicaciones (FICCT)
* **Carrera:** Ingeniería Informática / Ingeniería en Sistemas
* **Materia:** Sistemas de Información Geográfica (INF422 / SIG)
* **Docente:** Ing. Ubaldo Pérez Ferreira
* **Sistema:** VisorDatosSIG 2026 (Segunda Parte: Módulo Especializado de Agua Potable)
* **Semestre:** 2/2026 — Santa Cruz de la Sierra, Bolivia

---

## 📖 Descripción General

**VisorDatosSIG 2026** es una solución corporativa integral diseñada para la administración territorial, visualización catastral, red vial urbana y control operativo del servicio de agua potable. Integra en una sola arquitectura un visor geográfico web interactivo de alto rendimiento, un módulo de planificación de rutas de corte y reconexión mediante el **algoritmo de Dijkstra sobre grafo vial**, una aplicación móvil para brigadas técnicas y una suite de escritorio para migración transaccional de archivos Shapefile ESRI.

Toda la cartografía e infraestructura opera bajo el sistema de referencia espacial **EPSG:4326 (WGS 84)** con tipos nativos `GEOMETRY` e indexación espacial en SQL Server 2022.

---

## 🏗️ Arquitectura del Sistema

El ecosistema aplica principios de **Clean Architecture** y separación en N-Capas:

```
+-----------------------------------------------------------------------------------------------+
|                                     VISORDATOSSIG 2026                                        |
+-----------------------------------------------------------------------------------------------+
|   [Portal Web MVC & Leaflet]        [Desktop ETL WinForms]        [App Móvil Flutter]         |
|              \                                 |                                 /            |
|               \                                |                                /             |
|                +-------------------------------+-------------------------------+              |
|                                                |                                              |
|                                  [API RESTful ASP.NET Core 8]                                 |
|                                                |                                              |
|                               [Capa de Aplicación & Dominio]                                  |
|                         (DTOs, DijkstraRoutingService, PBAC, Entities)                        |
|                                                |                                              |
|                            [Infraestructura & Repositorios Dapper]                            |
|                                                |                                              |
|                     [Microsoft SQL Server 2022 Spatial (SRID 4326)]                           |
+-----------------------------------------------------------------------------------------------+
```

---

## ✨ Componentes y Módulos Principales

### 1. 🗺️ Visor Cartográfico Territorial (Web GIS)
* **Capas vectoriales activas:** Manzanas urbanas (polígonos), Lotes catastrales (polígonos con titular y zonificación), Red de vías urbanas (ejes de calles clasificadas), Códigos Fijos / Vértices geodésicos (puntos con cota Z) y Conexiones de agua potable (medidores).
* **Herramientas de análisis espacial:** Identificación predial por clic/toque, regla de medición lineal métrica (m/km), cálculo de superficies poligonales ($m^2$ y hectáreas), localizador por sensor GPS ("Mi Ubicación"), salto a coordenadas geográficas WGS 84 y botón de restablecimiento de extensión (San Ignacio de Velasco).
* **Galería de mapas base:** OpenStreetMap, Carto Positron, Carto Dark y Esri Satelital.
* **Filtros y Búsqueda:** Búsqueda alfanumérica rápida, sincronización bidireccional tabla-mapa, exportación de atributos a CSV y copia de geometrías a formato WKT.
* **Diseño adaptativo móvil:** Barra de herramientas inferior táctil (*Mobile Quick Dock*) y panel de detalles deslizable (*Bottom Sheet Drawer*).

### 2. 💧 Módulo de Cortes y Reconexiones de Agua Potable
* **Semaforización de conexiones:**
  * 🟢 **Normal:** Servicio al día sin mora.
  * 🟡 **Para Corte:** Servicio en mora susceptible a corte físico.
  * 🔴 **Cortado:** Servicio suspendido con válvula precintada.
  * ⚪ **Baja Parcial:** Medidor retirado o precintado temporal.
* **Planificador de Rutas Óptimas (Algoritmo de Dijkstra):** Generación de rutas mínimas recorriendo estrictamente la red de calles y vías urbanas (sin atravesar manzanas a campo traviesa), ordenando secuencialmente las paradas del $1$ al $N$.
* **Dashboard Gerencial:** Indicadores clave de desempeño (KPIs de mora, conexiones totales, cortes ejecutados y porcentaje de efectividad).
* **Monitoreo Satelital en Tiempo Real:** Rastreo en mapa de cuadrillas en ruta, porcentaje de batería del móvil, estado operativo (Disponible/En Ruta) y nivel de avance de la orden.

### 3. 📱 Aplicación Móvil para Cuadrillas de Campo (`04_Movil` - Flutter)
* Diseñada para teléfonos y tablets Android e iOS.
* **Bandeja de trabajo diario:** Carga de asignaciones de corte y reconexión emitidas por la central.
* **Navegación en mapa:** Visualización de la ruta óptima secuencial paso a paso.
* **Ficha de terreno:** Registro de lectura actual del medidor, verificación del número de serie físico, selección del estado (Ejecutado, Casa Cerrada, Imposible, Rechazado) y notas de campo.
* **Evidencia fotográfica y GPS Antifraude:** Captura obligatoria de foto del medidor y verificación automática de coordenadas satelitales para evitar registros fraudulentos a distancia.

### 4. 🖥️ Herramienta de Escritorio ETL (`VisorDatosSIG.Migrador` - WinForms)
* Aplicación de alto rendimiento desarrollada en C# .NET 8 WinForms.
* Ingesta e inspección binaria nativa de shapefiles ESRI (`.shp`, `.shx`, `.dbf`, `.prj`).
* Validación criptográfica y geométrica estricta de la proyección **WGS 84 (SRID 4326)**.
* Previsualización tabular de registros y mapeo de atributos de la tabla DBF a columnas de base de datos.
* Modalidades de carga: **Reemplazar** (limpieza y carga fresca) vs. **Anexar** (carga incremental).
* Carga transaccional por lotes (*Batch Processing*) con reversión total (*Rollback*) automática ante fallas.

### 5. 🔐 Administración, Seguridad y Auditoría (Backoffice)
* Control de acceso granular **PBAC** (*Permission-Based Access Control*) con *Feature Flags* activables por usuario para capas, herramientas y módulos.
* Cifrado criptográfico de contraseñas mediante **PBKDF2-HMAC-SHA512** (100,000 iteraciones + Salt).
* Protección contra ataques de fuerza bruta (bloqueo automático de 15 minutos tras 5 fallos consecutivos).
* Bitácora de auditoría inmutable que registra inicios de sesión, consultas espaciales, migraciones y modificaciones de permisos con IP y estampilla de tiempo.

---

## 📂 Estructura del Repositorio

```
├── 01_BaseDatos/
│   ├── 01_CrearBD.sql                         # Creación de base de datos y catálogos base
│   ├── 02_Objetos.sql                         # Tablas espaciales, vistas e índices espaciales
│   ├── 03_Seguridad.sql                       # Roles, permisos PBAC y funciones criptográficas
│   ├── 04_DatosPrueba.sql                     # Datos semilla cartográficos
│   ├── 05_ModuloAguaCortesReconexiones.sql    # Estructuras y stored procedures del módulo de agua
│   └── init-db.sh                             # Script de inicialización automática en Docker
├── 02_CSharp/
│   ├── VisorDatosSIG.Domain/                  # Entidades de dominio, enums y value objects
│   ├── VisorDatosSIG.Application/             # Servicios de negocio, DTOs y Dijkstra
│   ├── VisorDatosSIG.Infrastructure/          # Repositorios espaciales y lector de Shapefiles
│   ├── VisorDatosSIG.Api/                     # API RESTful en ASP.NET Core 8
│   ├── VisorDatosSIG.Migrador/                # Herramienta de escritorio ETL WinForms
│   └── VisorDatosSIG.Tests/                   # Pruebas unitarias y de integración
├── 03_Frontend/                               # Aplicación Web ASP.NET Core MVC + Leaflet.js
│   ├── Controllers/                           # Controladores MVC y API interna
│   ├── Views/                                 # Vistas Razor (Visor, Agua, Admin)
│   └── wwwroot/                               # Librerías JS, CSS, iconos y estilos Leaflet
├── 04_Movil/                                  # Aplicación Móvil en Flutter (Android / iOS)
│   ├── lib/                                   # Código Dart (screens, models, services)
│   └── pubspec.yaml                           # Dependencias Flutter
├── docker-compose.yml                         # Despliegue contenerizado de SQL Server 2022
├── DOCUMENTACION.md                           # Documento completo de Arquitectura y Diseño UML
├── MANUAL_DE_USUARIO.md                       # Manual de Usuario Oficial paso a paso
└── VisorDatosSIG.sln                          # Solución principal de Visual Studio 2022 / .NET 8
```

---

## 🚀 Puesta en Marcha y Ejecución

### Requisitos Previos
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.
* [Docker Desktop](https://www.docker.com/) o una instancia local de **Microsoft SQL Server 2022**.
* [Flutter SDK 3.x](https://flutter.dev/docs/get-started/install) (para compilar la aplicación móvil).

### 1. Inicialización de la Base de Datos con Docker
Puede levantar el servidor SQL Server 2022 con los scripts precargados ejecutando:
```bash
docker compose up -d
```
O ejecutar secuencialmente los scripts de la carpeta `01_BaseDatos/` en SQL Server Management Studio (SSMS) o Azure Data Studio:
1. `01_CrearBD.sql`
2. `02_Objetos.sql`
3. `03_Seguridad.sql`
4. `04_DatosPrueba.sql`
5. `05_ModuloAguaCortesReconexiones.sql`

### 2. Ejecutar la Aplicación Web (Visor y Módulo de Agua)
```bash
cd 03_Frontend
dotnet run
```
Abra el navegador en `http://localhost:5000` o la URL mostrada en consola.

### 3. Ejecutar la Herramienta de Escritorio ETL (Migrador)
En Windows:
```bash
cd 02_CSharp/VisorDatosSIG.Migrador
dotnet run
```
O ejecutar directamente el binario generado `VisorDatosSIG.Migrador.exe`.

### 4. Ejecutar la Aplicación Móvil (Flutter)
Conecte su dispositivo móvil o inicie un emulador y ejecute:
```bash
cd 04_Movil
flutter pub get
flutter run
```

---

## 🔑 Credenciales de Acceso Predeterminadas

| Módulo | Usuario | Contraseña | Rol / Perfil |
| :--- | :--- | :--- | :--- |
| **Portal Web** | `admin` | `Admin123*!` | Administrador Total (Todas las capas y módulos) |
| **Portal Web** | `consultor` | `Consultor123*!` | Consultor SIG (Capas y consultas cartográficas) |
| **App Móvil** | `TRAB-001` | `123456` | Cortador — Juan Pérez Morales (Moto) |
| **App Móvil** | `TRAB-002` | `123456` | Cortador — Roberto Gómez Tapia (Moto) |
| **App Móvil** | `TRAB-003` | `123456` | Reconector — Marcos Suárez Vaca (Moto) |
| **App Móvil** | `TRAB-004` | `123456` | Reconector — David Rojas Peña (A pie) |

---

## 📚 Documentación Adicional

* 📘 [Manual de Usuario Oficial (MANUAL_DE_USUARIO.md)](MANUAL_DE_USUARIO.md): Guía ilustrada paso a paso para operadores, supervisores y administradores.
* 📋 [Documentación Técnica de Software (DOCUMENTACION.md)](DOCUMENTACION.md): Especificación de arquitectura, diagramas de clases UML, diagramas de secuencia, modelo entidad-relación y contratos de API GeoJSON.

---

## 👨‍💻 Autor y Licencia

Desarrollado para la materia de **Sistemas de Información Geográfica** — UAGRM.  
Repositorio Oficial: [https://github.com/BernardoChavez/VisorDatosSIG](https://github.com/BernardoChavez/VisorDatosSIG)
