# 🛠️ GUÍA DE EJECUCIÓN: SCRIPTS PARA ENTERPRISE ARCHITECT (EA)
## Proyecto: VisorDatosSIG 2026 — UAGRM FICCT

Esta carpeta contiene los scripts automatizados en **JScript / JavaScript** diseñados específicamente para ejecutarse dentro de **Sparx Systems Enterprise Architect (EA)**, permitiendo generar con un solo clic todos los diagramas faltantes requeridos para la documentación técnica y de usuario del sistema.

---

### 📁 Archivos Disponibles en este Paquete:

1. **`Crear_Diagramas_Comunicacion.js`**:
   * Genera el paquete **2.3.2. Diagramas de Comunicación**.
   * Crea automáticamente los **11 diagramas de comunicación** con estereotipos **Boundary, Control y Entity (BCE / Robustness)**.
   * Modela los casos de uso: `CU01`, `CU02`, `CU05`, `CU06`, `CU08`, `CU11`, `CU13`, `CU14`, `CU15`, `CU16` y `CU17`.
   * Vincula los enlaces de comunicación y los mensajes numerados secuencialmente.

2. **`Crear_Diagramas_Secuencia.js`**:
   * Genera el paquete **2.4.3. Diagramas de Secuencia**.
   * Crea automáticamente los **9 diagramas de secuencia** con sus correspondientes Lifelines (`Actor`, `Vista/Boundary`, `Controlador/API`, `Servicio`, `Repositorio` y `Base de Datos SQL Server Spatial`).
   * Genera todos los mensajes sincrónicos y de retorno numerados paso a paso.

3. **`Crear_Diagramas_Arquitectura.js`**:
   * Genera el paquete **2.4.1. Diagramas de Arquitectura**.
   * Crea:
     - **Diagrama de Despliegue Físico:** Nodos Cliente Web, Cliente Móvil Flutter, Servidor Web Kestrel/Linux y Servidor SQL Server 2022 con protocolos y puertos.
     - **Diagrama de Diseño Lógico en Capas:** Presentación, Servicios API, Aplicación, Dominio, Infraestructura y Persistencia Espacial.
     - **Diagrama de Componentes:** Controllers, DijkstraRoutingService, ShapefileParser, SpatialRepository y SQL Server Spatial.

4. **`Diagramas_PlantUML_Completos.puml`**:
   * Archivo de respaldo con la especificación completa en sintaxis **PlantUML**.
   * Permite previsualizar todos los diagramas de manera instantánea en VS Code (con la extensión PlantUML) o en visores online como [planttext.com](https://www.planttext.com/).

---

### 🚀 Pasos para Ejecutar los Scripts dentro de Enterprise Architect:

1. **Abrir su Proyecto en Enterprise Architect:**
   * Inicie Enterprise Architect y abra su archivo de modelo (`.qea`, `.feap` o `.eapx`).
2. **Seleccionar el Paquete de Destino:**
   * En el panel lateral izquierdo (**Project Browser** / Árbol del Proyecto), haga clic sobre el paquete raíz o sobre la sección donde desea crear los diagramas (por ejemplo: `2.3. Flujo de trabajo Análisis` o `2.4. Flujo de trabajo Diseño`).
3. **Abrir la Ventana de Scripting:**
   * Vaya al menú superior:
     * En EA v15 / v16 / v17: **Specialize** $ightarrow$ **Tools** $ightarrow$ **Scripting** (o **Develop** $ightarrow$ **Source** $ightarrow$ **Scripting**).
     * En EA v14 o anteriores: **Tools** $ightarrow$ **Scripting**.
4. **Crear o Cargar el Script:**
   * En la ventana *Scripting*, haga clic derecho sobre el grupo **Project Browser Scripts** (o **User Scripts**) y seleccione **"New JScript"**.
   * Asígnele un nombre (por ejemplo: `GenerarDiagramasComunicacion`).
5. **Pegar el Código y Ejecutar:**
   * Abra el archivo `Crear_Diagramas_Comunicacion.js`, copie todo su contenido y péguelo dentro del editor de scripts de EA reemplazando cualquier plantilla existente.
   * Haga clic en el botón **Run** (icono de play verde ▶️ o presione `Ctrl + F5`).
   * En la ventana *System Output* observará la traza de creación de cada diagrama y elemento.
   * Repita el mismo procedimiento para `Crear_Diagramas_Secuencia.js` y `Crear_Diagramas_Arquitectura.js`.
6. **Exportar las Imágenes para Word:**
   * En el Project Browser de EA, abra el diagrama generado.
   * Presione **`Ctrl + T`** (o clic derecho sobre el lienzo del diagrama $ightarrow$ *Save Diagram as Image*).
   * Guarde la imagen en formato PNG (ej. `CU01_Comunicacion.png`).
   * Pegue la imagen directamente en el documento de Word en la sección correspondiente.

---

### 📝 Cómo Mejorar y Finalizar los Dos Documentos de Word (`.docx`):

#### 1. Para `VisorDatosSIG_Documentacion.docx`:
* Ya tiene 17 imágenes incrustadas (contexto, casos de uso, ERD, etc.).
* Al correr estos scripts en EA y exportar las imágenes, solo debe reemplazar los marcadores de texto:
  - Reemplace los 11 marcadores `📊 [DIAGRAMA DE COMUNICACIÓN - CUX]` con las imágenes de comunicación.
  - Reemplace los 9 marcadores `📊 [DIAGRAMA DE SECUENCIA - CUX]` con las imágenes de secuencia.
  - Reemplace los marcadores de Despliegue, Capas y Componentes.

#### 2. Para `Manual_Usuario_VisorDatosSIG_2026.docx`:
* Actualmente contiene 452 párrafos de texto estructurado y exhaustivo en 9 capítulos, pero tiene **0 imágenes**.
* **Mejoras necesarias:**
  - Incrustar capturas de pantalla de la interfaz web (Lienzo Leaflet, capas activas, herramientas de medición, búsqueda).
  - Incrustar capturas del módulo de agua (Dashboard de KPIs, asignación con Dijkstra, monitoreo satelital en vivo).
  - Incrustar capturas de la aplicación móvil Flutter (Login del operario, mapa de ruta, ficha de registro de lectura y foto).
  - Incrustar captura del software de escritorio ETL (WinForms Migrador).
