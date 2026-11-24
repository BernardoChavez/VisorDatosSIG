# Aplicación Móvil SIG - Gestión de Cortes y Reconexiones de Agua Potable

Aplicación móvil desarrollada en **Flutter** para operarios en campo (**Cortadores** y **Reconectores**), cumpliendo al 100% con los requerimientos de la materia **Sistemas de Información Geográfica (UAGRM 2/2026)**.

---

## 📱 Funcionalidades Implementadas

### 1. Acceso y Autenticación
- Pantalla de inicio de sesión con usuario y contraseña del operario (`Cortador` / `Reconector`).
- Selector rápido con los operarios registrados en la base de datos (ej. `TRAB-001` - Juan Pérez Morales).
- Botón de **Ajustes de Servidor** para configurar la IP de la red Wi-Fi local en tiempo real sin recompilar.

### 2. Lista de Tareas Secuencial (Dijkstra 1, 2, 3...)
- Muestra la ruta activa asignada al operario en orden óptimo calculado por el **Algoritmo de Dijkstra** sobre la red de calles (`dbo.Vias`), sin atravesar manzanas.
- Indicadores de progreso (total asignados, completados, pendientes, distancia acumulada).
- Badges visuales con el número de secuencia (`#1`, `#2`, `#3...`) y chip de estado (*Pendiente*, *Ejecutado*, *Casa Cerrada*).

### 3. Navegación y Georreferenciación
- Muestra en tiempo real:
  - Coordenadas del destino (medidor de agua).
  - Coordenadas GPS actuales del operario y precisión satelital.
  - Distancia geodésica exacta en metros al medidor.
  - Indicador de proximidad con tolerancia de **60 metros**.
- **Botón "Navegar con Instrucciones Giro a Giro":**
  - Permite seleccionar el modo de desplazamiento: 🛵 **Moto**, 🚗 **Vehículo**, 🚶 **A pie**.
  - Abre la aplicación de navegación (Google Maps / Waze) con indicaciones paso a paso y guiado por voz.
- **Botón "Ver Ruta en Mapa":**
  - Visualizador cartográfico interactivo integrado (`flutter_map` con OpenStreetMap).
  - Dibuja la polilínea óptima de la red vial calculada por Dijkstra y los marcadores numerados de cada parada.

### 4. Registro en Campo y Validación de Asistencia
- **Captura Fotográfica:** Tomar fotografía del medidor con la cámara del dispositivo o cargar desde la galería.
- **Lectura del Medidor:** Registro del número de medidor y lectura actual en metros cúbicos ($m^3$).
- **Escaneo OCR (Opcional):** Reconocimiento automático de dígitos del medidor desde la imagen.
- **Reporte de Observaciones:** Selección rápida de causas en caso de imposibilidad (*Casa Cerrada*, *Perro Bravo*, *Reja con Candado*).
- **Validación Automática por GPS ($\le 60$ m):**
  - Al guardar, la app transmite automáticamente latitud, longitud, precisión y timestamp.
  - El servidor valida mediante cálculo geodésico que el trabajador esté a menos de **60 metros** del medidor. Si se encuentra a mayor distancia, el sistema rechaza la operación para prevenir reportes fraudulentos de "casa cerrada".

### 5. Telemetría y Monitoreo en Tiempo Real
- Envío automático en segundo plano de la posición GPS del operario hacia el backend (`/api/agua/monitoreo/ubicacion`).
- El administrador puede ver en vivo en la plataforma Web el desplazamiento del trabajador sobre el mapa cartográfico.

---

## 🚀 Conexión con el Servidor Backend (Wi-Fi Local)

1. **Asegúrate de que la PC y el celular estén conectados a la misma red Wi-Fi.**
2. En la PC, la IP local detectada es:
   ```
   192.168.0.9
   ```
3. El backend ASP.NET Core está configurado para escuchar en todas las interfaces de red (`http://0.0.0.0:5000`).
4. Inicia el servidor backend en la PC:
   ```bash
   dotnet run --project proyecto/03_Frontend/VisorDatosSIG.Web.csproj
   ```
5. En la aplicación móvil, la URL base predeterminada ya apunta a:
   ```
   http://192.168.0.9:5000
   ```
   *(Si tu IP cambia, puedes modificarla en la pantalla de Login tocando el ícono de engranaje ⚙️ en la esquina superior derecha).*

---

## 🛠️ Cómo Probar o Compilar la App en tu Celular

### Ejecutar en modo desarrollo con el celular conectado por USB:
```bash
cd proyecto/04_Movil
flutter run
```

### Generar el archivo APK para instalar directamente en Android:
```bash
cd proyecto/04_Movil
flutter build apk --release
```
El instalador quedará generado en:
`proyecto/04_Movil/build/app/outputs/flutter-apk/app-release.apk`

---

## 🧪 Pruebas Automatizadas

Para ejecutar las pruebas unitarias y de widgets de la aplicación móvil:
```bash
cd proyecto/04_Movil
flutter test
```
*Resultado: 4/4 pruebas unitarias aprobadas.*
