#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisorDatosSIG.Application.DTOs.Migracion;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain.Enums;
using VisorDatosSIG.Infrastructure.Data;
using VisorDatosSIG.Infrastructure.Repositories;
using VisorDatosSIG.Infrastructure.Shapefiles;

namespace VisorDatosSIG.Migrador.Forms;

public partial class MainMigradorForm : Form
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IMigradorService _migradorService;
    private readonly IShapefileReader _shapefileReader;
    private readonly IMigracionRepository _migracionRepo;
    private readonly IAuditoriaService _auditoriaService;
    private CancellationTokenSource? _cts;

    // Controles de UI
    private TextBox txtServidor = null!;
    private TextBox txtBaseDatos = null!;
    private CheckBox chkSeguridadIntegrada = null!;
    private TextBox txtUsuarioSql = null!;
    private TextBox txtPasswordSql = null!;
    private Button btnProbarConexion = null!;
    private Label lblEstadoConexion = null!;

    private TextBox txtRutaOrigen = null!;
    private Button btnExaminarCarpeta = null!;
    private Button btnExaminarArchivo = null!;
    private Button btnGenerarEjemplos = null!;

    private DataGridView dgvCapasDetectadas = null!;
    private DataGridView dgvVistaPrevia = null!;
    private DataGridView dgvMapeo = null!;
    private DataGridView dgvHistorial = null!;

    private RadioButton rbReemplazar = null!;
    private RadioButton rbAnexar = null!;
    private TextBox txtUsuarioMigrador = null!;
    private Button btnIniciarMigracion = null!;
    private Button btnCancelarMigracion = null!;
    private ProgressBar pbProgreso = null!;
    private Label lblProgresoPorcentaje = null!;
    private Label lblEstadoOperacion = null!;

    private Label lblTotalStats = null!;
    private Label lblExitososStats = null!;
    private Label lblOmitidosStats = null!;
    private Label lblFallidosStats = null!;
    private Label lblTiempoStats = null!;

    private ListBox lstLog = null!;
    private Button btnExportarLog = null!;
    private Button btnLimpiarLog = null!;

    private readonly List<ValidacionShapefileDto> _capasDetectadas = new();

    public MainMigradorForm()
    {
        InitializeComponent();

        _dbFactory = new DbConnectionFactory();
        var bitacoraRepo = new BitacoraRepository(_dbFactory);
        _auditoriaService = new AuditoriaService(bitacoraRepo);
        _migracionRepo = new MigracionRepository(_dbFactory);
        _shapefileReader = new ShapefileReader();
        _migradorService = new MigradorService(_migracionRepo, _shapefileReader, _auditoriaService);

        Log("Sistema de Migración VisorDatosSIG 2026 inicializado correctamente.");
    }

    private void InitializeComponent()
    {
        this.Text = "VisorDatosSIG 2026 — Módulo de Migración y Validación de Shapefiles";
        this.Size = new Size(1150, 800);
        this.MinimumSize = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(245, 247, 250);

        // Panel Superior (Encabezado)
        var panelHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(15, 10, 15, 10)
        };

        var lblTitulo = new Label
        {
            Text = "VisorDatosSIG 2026 — Ingesta Espacial de Shapefiles a SQL Server",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, 12)
        };

        var lblSubtitulo = new Label
        {
            Text = "Validación rigurosa de geometrías WGS 84 (SRID 4326), componentes SHP/SHX/DBF/PRJ y transacciones ACID.",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(200, 215, 235),
            AutoSize = true,
            Location = new Point(16, 38)
        };

        panelHeader.Controls.Add(lblTitulo);
        panelHeader.Controls.Add(lblSubtitulo);
        this.Controls.Add(panelHeader);

        // Contenedor Principal con Scroll
        var mainContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(15)
        };
        this.Controls.Add(mainContainer);

        // 1. GroupBox Conexión SQL
        var gbConexion = new GroupBox
        {
            Text = " 1. Configuración de Conexión a Base de Datos (SQL Server 2022) ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 43, 73),
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(10)
        };

        var lblServ = new Label { Text = "Servidor:", Location = new Point(15, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        txtServidor = new TextBox { Text = ".\\SQLEXPRESS", Location = new Point(75, 25), Width = 150, Font = new Font("Segoe UI", 9f) };

        var lblBd = new Label { Text = "Base Datos:", Location = new Point(235, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        txtBaseDatos = new TextBox { Text = "VisorDatosSIG_BD", Location = new Point(310, 25), Width = 150, Font = new Font("Segoe UI", 9f) };

        chkSeguridadIntegrada = new CheckBox { Text = "Seguridad Integrada (Windows)", Checked = true, Location = new Point(475, 26), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        chkSeguridadIntegrada.CheckedChanged += (s, e) =>
        {
            txtUsuarioSql.Enabled = !chkSeguridadIntegrada.Checked;
            txtPasswordSql.Enabled = !chkSeguridadIntegrada.Checked;
        };

        var lblUsr = new Label { Text = "Usuario:", Location = new Point(680, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        txtUsuarioSql = new TextBox { Location = new Point(735, 25), Width = 90, Enabled = false, Font = new Font("Segoe UI", 9f) };

        var lblPass = new Label { Text = "Clave:", Location = new Point(835, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        txtPasswordSql = new TextBox { Location = new Point(880, 25), Width = 90, UseSystemPasswordChar = true, Enabled = false, Font = new Font("Segoe UI", 9f) };

        btnProbarConexion = new Button
        {
            Text = "Probar Conexión",
            Location = new Point(15, 65),
            Width = 140,
            Height = 32,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnProbarConexion.FlatAppearance.BorderSize = 0;
        btnProbarConexion.Click += async (s, e) => await ProbarConexionAsync();

        lblEstadoConexion = new Label
        {
            Text = "Estado: No verificado",
            Location = new Point(165, 72),
            AutoSize = true,
            ForeColor = Color.DarkSlateGray,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic)
        };

        gbConexion.Controls.AddRange(new Control[] {
            lblServ, txtServidor, lblBd, txtBaseDatos, chkSeguridadIntegrada,
            lblUsr, txtUsuarioSql, lblPass, txtPasswordSql, btnProbarConexion, lblEstadoConexion
        });

        // 2. GroupBox Origen de Datos
        var gbOrigen = new GroupBox
        {
            Text = " 2. Selección de Origen de Datos Shapefiles (Manzanas, Lotes, Códigos Fijos, Vías) ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 43, 73),
            Dock = DockStyle.Top,
            Height = 85,
            Padding = new Padding(10)
        };

        var lblRuta = new Label { Text = "Carpeta / Archivo:", Location = new Point(15, 32), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        txtRutaOrigen = new TextBox { Location = new Point(135, 29), Width = 550, Font = new Font("Segoe UI", 9f) };

        btnExaminarCarpeta = new Button
        {
            Text = "Examinar Carpeta...",
            Location = new Point(695, 27),
            Width = 140,
            Height = 30,
            BackColor = Color.FromArgb(235, 240, 248),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f)
        };
        btnExaminarCarpeta.Click += (s, e) => ExaminarCarpeta();

        btnExaminarArchivo = new Button
        {
            Text = "Seleccionar .SHP...",
            Location = new Point(845, 27),
            Width = 135,
            Height = 30,
            BackColor = Color.FromArgb(235, 240, 248),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f)
        };
        btnExaminarArchivo.Click += (s, e) => ExaminarArchivo();

        btnGenerarEjemplos = new Button
        {
            Text = "Generar Shapefiles de Prueba",
            Location = new Point(990, 27),
            Width = 190,
            Height = 30,
            BackColor = Color.FromArgb(25, 135, 84),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        btnGenerarEjemplos.FlatAppearance.BorderSize = 0;
        btnGenerarEjemplos.Click += (s, e) => GenerarShapefilesEjemplo();

        gbOrigen.Controls.AddRange(new Control[] {
            lblRuta, txtRutaOrigen, btnExaminarCarpeta, btnExaminarArchivo, btnGenerarEjemplos
        });

        // 3. GroupBox Capas Detectadas
        var gbCapas = new GroupBox
        {
            Text = " 3. Validación de Componentes de Capa (.shp, .shx, .dbf, .prj) y Proyección ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 43, 73),
            Dock = DockStyle.Top,
            Height = 150,
            Padding = new Padding(10)
        };

        dgvCapasDetectadas = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 8.5f)
        };

        dgvCapasDetectadas.Columns.Add("Capa", "Capa Reconocida");
        dgvCapasDetectadas.Columns.Add("Archivo", "Archivo Principal");
        dgvCapasDetectadas.Columns.Add("SHP", ".SHP");
        dgvCapasDetectadas.Columns.Add("SHX", ".SHX");
        dgvCapasDetectadas.Columns.Add("DBF", ".DBF");
        dgvCapasDetectadas.Columns.Add("PRJ", ".PRJ (WGS84)");
        dgvCapasDetectadas.Columns.Add("TipoGeometria", "Geometría");
        dgvCapasDetectadas.Columns.Add("Registros", "Nº Registros");
        dgvCapasDetectadas.Columns.Add("Estado", "Estado Validación");

        dgvCapasDetectadas.SelectionChanged += async (s, e) => await ActualizarVistaPreviaAsync();

        gbCapas.Controls.Add(dgvCapasDetectadas);

        // 4. TabControl de Operaciones
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f)
        };

        // Tab A: Ejecución y Estadísticas
        var tabEjecucion = new TabPage("  Ejecución de Migración  ") { BackColor = Color.White, Padding = new Padding(15) };
        ConfigurarTabEjecucion(tabEjecucion);

        // Tab B: Vista Previa y Mapeo
        var tabVistaPrevia = new TabPage("  Previsualización y Mapeo de Campos  ") { BackColor = Color.White, Padding = new Padding(15) };
        ConfigurarTabVistaPrevia(tabVistaPrevia);

        // Tab C: Bitácora en Vivo
        var tabLog = new TabPage("  Bitácora de Eventos  ") { BackColor = Color.White, Padding = new Padding(15) };
        ConfigurarTabLog(tabLog);

        // Tab D: Historial en BD
        var tabHistorial = new TabPage("  Historial en Base de Datos  ") { BackColor = Color.White, Padding = new Padding(15) };
        ConfigurarTabHistorial(tabHistorial);

        tabControl.TabPages.AddRange(new TabPage[] { tabEjecucion, tabVistaPrevia, tabLog, tabHistorial });

        // Ensamblar orden de controles
        var panelCentral = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        panelCentral.Controls.Add(tabControl);

        mainContainer.Controls.Add(panelCentral);
        mainContainer.Controls.Add(gbCapas);
        mainContainer.Controls.Add(gbOrigen);
        mainContainer.Controls.Add(gbConexion);
    }

    private void ConfigurarTabEjecucion(TabPage tab)
    {
        var panelOpciones = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(5) };

        var lblModo = new Label { Text = "Modalidad de Carga:", Location = new Point(10, 12), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        rbReemplazar = new RadioButton { Text = "Reemplazar capa existente (limpieza previa)", Checked = true, Location = new Point(160, 10), AutoSize = true };
        rbAnexar = new RadioButton { Text = "Anexar registros (evitar duplicados por clave natural)", Location = new Point(480, 10), AutoSize = true };

        var lblUsuario = new Label { Text = "Usuario ejecutor:", Location = new Point(10, 42), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        txtUsuarioMigrador = new TextBox { Text = "admin", Location = new Point(160, 39), Width = 150 };

        panelOpciones.Controls.AddRange(new Control[] { lblModo, rbReemplazar, rbAnexar, lblUsuario, txtUsuarioMigrador });

        var panelBotones = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(5) };

        btnIniciarMigracion = new Button
        {
            Text = "▶ Iniciar Migración Transaccional",
            Location = new Point(10, 10),
            Width = 260,
            Height = 40,
            BackColor = Color.FromArgb(25, 135, 84),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };
        btnIniciarMigracion.FlatAppearance.BorderSize = 0;
        btnIniciarMigracion.Click += async (s, e) => await IniciarMigracionAsync();

        btnCancelarMigracion = new Button
        {
            Text = "✖ Cancelar",
            Location = new Point(285, 10),
            Width = 140,
            Height = 40,
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = false,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        btnCancelarMigracion.FlatAppearance.BorderSize = 0;
        btnCancelarMigracion.Click += (s, e) => CancelarMigracion();

        panelBotones.Controls.AddRange(new Control[] { btnIniciarMigracion, btnCancelarMigracion });

        var panelProgreso = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(5) };

        pbProgreso = new ProgressBar { Location = new Point(10, 10), Width = 650, Height = 25 };
        lblProgresoPorcentaje = new Label { Text = "0%", Location = new Point(670, 14), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        lblEstadoOperacion = new Label { Text = "Listo para iniciar migración.", Location = new Point(10, 42), AutoSize = true, ForeColor = Color.DarkSlateGray };

        panelProgreso.Controls.AddRange(new Control[] { pbProgreso, lblProgresoPorcentaje, lblEstadoOperacion });

        // Tarjetas de Estadísticas
        var gbStats = new GroupBox
        {
            Text = " Métricas de la Ejecución ",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(15)
        };

        var flowStats = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };

        lblTotalStats = CrearCardStat("Total Registros", "0", Color.FromArgb(13, 110, 253));
        lblExitososStats = CrearCardStat("Exitosos (Insertados)", "0", Color.FromArgb(25, 135, 84));
        lblOmitidosStats = CrearCardStat("Omitidos (Duplicados)", "0", Color.FromArgb(255, 193, 7));
        lblFallidosStats = CrearCardStat("Fallidos / Errores", "0", Color.FromArgb(220, 53, 69));
        lblTiempoStats = CrearCardStat("Duración Total", "0 ms", Color.FromArgb(108, 117, 125));

        flowStats.Controls.AddRange(new Control[] {
            lblTotalStats.Parent!, lblExitososStats.Parent!, lblOmitidosStats.Parent!, lblFallidosStats.Parent!, lblTiempoStats.Parent!
        });

        gbStats.Controls.Add(flowStats);

        tab.Controls.Add(gbStats);
        tab.Controls.Add(panelProgreso);
        tab.Controls.Add(panelBotones);
        tab.Controls.Add(panelOpciones);
    }

    private Label CrearCardStat(string titulo, string valorInicial, Color color)
    {
        var panel = new Panel
        {
            Width = 180,
            Height = 90,
            BackColor = Color.FromArgb(248, 249, 250),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(5, 5, 15, 5)
        };

        var lblTit = new Label
        {
            Text = titulo,
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.DarkSlateGray
        };

        var lblVal = new Label
        {
            Text = valorInicial,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = color
        };

        panel.Controls.Add(lblVal);
        panel.Controls.Add(lblTit);

        return lblVal;
    }

    private void ConfigurarTabVistaPrevia(TabPage tab)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 250
        };

        // Panel superior: DataGridView con los primeros 20 registros
        var gbGrid = new GroupBox
        {
            Text = " Previsualización de Primeros 20 Registros del Shapefile Seleccionado ",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        dgvVistaPrevia = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 8.5f)
        };
        gbGrid.Controls.Add(dgvVistaPrevia);

        // Panel inferior: Mapeo de campos
        var gbMapeo = new GroupBox
        {
            Text = " Mapeo de Campos DBF Origen → Columnas SQL Server Destino ",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        dgvMapeo = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 8.5f)
        };
        dgvMapeo.Columns.Add("CampoDbf", "Campo en Shapefile (.DBF)");
        dgvMapeo.Columns.Add("CampoSql", "Columna SQL Server Destino");
        dgvMapeo.Columns.Add("Tipo", "Tipo de Dato");
        dgvMapeo.Columns.Add("Requerido", "Obligatorio");

        gbMapeo.Controls.Add(dgvMapeo);

        split.Panel1.Controls.Add(gbGrid);
        split.Panel2.Controls.Add(gbMapeo);

        tab.Controls.Add(split);
    }

    private void ConfigurarTabLog(TabPage tab)
    {
        var panelBotonesLog = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5) };

        btnExportarLog = new Button
        {
            Text = "Exportar Bitácora a Archivo (TXT / CSV)",
            Location = new Point(5, 5),
            Width = 260,
            Height = 32,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnExportarLog.FlatAppearance.BorderSize = 0;
        btnExportarLog.Click += (s, e) => ExportarLog();

        btnLimpiarLog = new Button
        {
            Text = "Limpiar Pantalla",
            Location = new Point(275, 5),
            Width = 130,
            Height = 32,
            BackColor = Color.FromArgb(235, 240, 248),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f)
        };
        btnLimpiarLog.Click += (s, e) => lstLog.Items.Clear();

        panelBotonesLog.Controls.AddRange(new Control[] { btnExportarLog, btnLimpiarLog });

        lstLog = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9f),
            BackColor = Color.FromArgb(20, 25, 35),
            ForeColor = Color.FromArgb(220, 230, 242),
            IntegralHeight = false
        };

        tab.Controls.Add(lstLog);
        tab.Controls.Add(panelBotonesLog);
    }

    private void ConfigurarTabHistorial(TabPage tab)
    {
        var panelBotonesHistorial = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5) };

        var btnActualizar = new Button
        {
            Text = "⟳ Refrescar Historial desde Base de Datos",
            Location = new Point(5, 5),
            Width = 280,
            Height = 32,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnActualizar.FlatAppearance.BorderSize = 0;
        btnActualizar.Click += async (s, e) => await CargarHistorialAsync();

        panelBotonesHistorial.Controls.Add(btnActualizar);

        dgvHistorial = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 8.5f)
        };
        dgvHistorial.Columns.Add("Id", "ID");
        dgvHistorial.Columns.Add("Fecha", "Fecha Inicio");
        dgvHistorial.Columns.Add("Usuario", "Usuario");
        dgvHistorial.Columns.Add("Capa", "Capa");
        dgvHistorial.Columns.Add("Archivo", "Archivo");
        dgvHistorial.Columns.Add("Modalidad", "Modalidad");
        dgvHistorial.Columns.Add("Total", "Total");
        dgvHistorial.Columns.Add("Exitosos", "Exitosos");
        dgvHistorial.Columns.Add("Omitidos", "Omitidos");
        dgvHistorial.Columns.Add("Fallidos", "Fallidos");
        dgvHistorial.Columns.Add("Estado", "Estado");
        dgvHistorial.Columns.Add("Duracion", "Duración (ms)");

        tab.Controls.Add(dgvHistorial);
        tab.Controls.Add(panelBotonesHistorial);
    }

    #region Lógica de Operaciones
    private async Task ProbarConexionAsync()
    {
        try
        {
            btnProbarConexion.Enabled = false;
            lblEstadoConexion.Text = "Probando conexión...";
            lblEstadoConexion.ForeColor = Color.FromArgb(13, 110, 253);

            ActualizarCadenaConexion();

            using var conn = await _dbFactory.CrearConexionAbiertaAsync();
            lblEstadoConexion.Text = "✔ Conexión exitosa a SQL Server y base de datos VisorDatosSIG_BD.";
            lblEstadoConexion.ForeColor = Color.FromArgb(25, 135, 84);
            Log("Prueba de conexión a SQL Server exitosa.");
        }
        catch (Exception ex)
        {
            lblEstadoConexion.Text = "✖ Error de conexión: " + ex.Message;
            lblEstadoConexion.ForeColor = Color.FromArgb(220, 53, 69);
            Log($"Error al conectar a SQL Server: {ex.Message}");
            MessageBox.Show($"No se pudo conectar a la base de datos:\n\n{ex.Message}", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnProbarConexion.Enabled = true;
        }
    }

    private void ActualizarCadenaConexion()
    {
        var cadena = DbConnectionFactory.ConstruirCadena(
            txtServidor.Text.Trim(),
            txtBaseDatos.Text.Trim(),
            chkSeguridadIntegrada.Checked,
            txtUsuarioSql.Text.Trim(),
            txtPasswordSql.Text);

        _dbFactory.ConfigurarCadenaConexion(cadena);
    }

    private void ExaminarCarpeta()
    {
        using var fbd = new FolderBrowserDialog();
        fbd.Description = "Seleccione la carpeta que contiene los archivos Shapefile (.shp)";
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            txtRutaOrigen.Text = fbd.SelectedPath;
            EscanearCarpeta(fbd.SelectedPath);
        }
    }

    private void ExaminarArchivo()
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "ESRI Shapefiles (*.shp)|*.shp|Todos los archivos (*.*)|*.*";
        ofd.Title = "Seleccione un archivo Shapefile";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtRutaOrigen.Text = ofd.FileName;
            EscanearArchivo(ofd.FileName);
        }
    }

    private void EscanearCarpeta(string carpeta)
    {
        _capasDetectadas.Clear();
        dgvCapasDetectadas.Rows.Clear();

        if (!Directory.Exists(carpeta)) return;

        var shpFiles = Directory.GetFiles(carpeta, "*.shp", SearchOption.TopDirectoryOnly);
        Log($"Escaneando carpeta '{carpeta}': {shpFiles.Length} archivos .shp encontrados.");

        foreach (var shpFile in shpFiles)
        {
            var validacion = _shapefileReader.InspeccionarShapefile(shpFile);
            _capasDetectadas.Add(validacion);

            string estadoStr = validacion.EsValidoParaCarga ? "VÁLIDO (WGS84)" : (validacion.TienePrj && !validacion.EsWgs84 ? "PROYECCIÓN INVÁLIDA" : "INCOMPLETO");

            dgvCapasDetectadas.Rows.Add(
                validacion.CapaEnum?.ToString() ?? validacion.NombreCapaDetectada,
                Path.GetFileName(shpFile),
                validacion.TieneShp ? "OK" : "FALTA",
                validacion.TieneShx ? "OK" : "FALTA",
                validacion.TieneDbf ? "OK" : "FALTA",
                validacion.TienePrj ? (validacion.EsWgs84 ? "OK (WGS84)" : "NO WGS84") : "FALTA",
                validacion.TipoGeometria ?? "N/D",
                validacion.CantidadRegistros,
                estadoStr
            );
        }

        if (dgvCapasDetectadas.Rows.Count > 0)
        {
            dgvCapasDetectadas.Rows[0].Selected = true;
        }
    }

    private void EscanearArchivo(string rutaArchivo)
    {
        _capasDetectadas.Clear();
        dgvCapasDetectadas.Rows.Clear();

        if (!File.Exists(rutaArchivo)) return;

        var validacion = _shapefileReader.InspeccionarShapefile(rutaArchivo);
        _capasDetectadas.Add(validacion);

        string estadoStr = validacion.EsValidoParaCarga ? "VÁLIDO (WGS84)" : (validacion.TienePrj && !validacion.EsWgs84 ? "PROYECCIÓN INVÁLIDA" : "INCOMPLETO");

        dgvCapasDetectadas.Rows.Add(
            validacion.CapaEnum?.ToString() ?? validacion.NombreCapaDetectada,
            Path.GetFileName(rutaArchivo),
            validacion.TieneShp ? "OK" : "FALTA",
            validacion.TieneShx ? "OK" : "FALTA",
            validacion.TieneDbf ? "OK" : "FALTA",
            validacion.TienePrj ? (validacion.EsWgs84 ? "OK (WGS84)" : "NO WGS84") : "FALTA",
            validacion.TipoGeometria ?? "N/D",
            validacion.CantidadRegistros,
            estadoStr
        );

        if (dgvCapasDetectadas.Rows.Count > 0)
        {
            dgvCapasDetectadas.Rows[0].Selected = true;
        }
    }

    private void GenerarShapefilesEjemplo()
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datasets_Ejemplo");
            ShapefileGenerator.GenerarDatasetsDePrueba(dir);
            txtRutaOrigen.Text = dir;
            EscanearCarpeta(dir);
            Log($"Se generaron shapefiles de prueba completos (Manzanas, Lotes, Códigos Fijos y Vías) en '{dir}'.");
            MessageBox.Show($"Shapefiles de prueba generados exitosamente en:\n\n{dir}\n\nSe han detectado automáticamente las 4 capas para su migración.", "Generación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error al generar shapefiles de prueba: {ex.Message}");
            MessageBox.Show($"Error al generar datasets de prueba:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ActualizarVistaPreviaAsync()
    {
        if (dgvCapasDetectadas.SelectedRows.Count == 0) return;
        int idx = dgvCapasDetectadas.SelectedRows[0].Index;
        if (idx < 0 || idx >= _capasDetectadas.Count) return;

        var capa = _capasDetectadas[idx];
        await CargarVistaPreviaGridAsync(capa.RutaShp);
        CargarMapeoGrid(capa);
    }

    private async Task CargarVistaPreviaGridAsync(string rutaShp)
    {
        dgvVistaPrevia.Columns.Clear();
        dgvVistaPrevia.Rows.Clear();

        try
        {
            var registros = await _migradorService.ObtenerVistaPreviaAsync(rutaShp, 20);
            if (registros.Count == 0) return;

            dgvVistaPrevia.Columns.Add("Fila", "#");
            dgvVistaPrevia.Columns.Add("OrigenId", "ID Origen");
            dgvVistaPrevia.Columns.Add("Geometria", "Resumen WKT Geometría");

            var first = registros.First();
            foreach (var key in first.Atributos.Keys)
            {
                dgvVistaPrevia.Columns.Add(key, key);
            }

            foreach (var r in registros)
            {
                var row = new List<object?> { r.IndiceFila, r.OrigenId, r.WktResumen };
                foreach (var key in first.Atributos.Keys)
                {
                    r.Atributos.TryGetValue(key, out var val);
                    row.Add(val);
                }
                dgvVistaPrevia.Rows.Add(row.ToArray());
            }
        }
        catch (Exception ex)
        {
            Log($"Error al cargar vista previa: {ex.Message}");
        }
    }

    private void CargarMapeoGrid(ValidacionShapefileDto capa)
    {
        dgvMapeo.Rows.Clear();

        switch (capa.CapaEnum)
        {
            case TipoCapa.Manzanas:
                dgvMapeo.Rows.Add("COD_MZ / MZ", "CodigoManzana", "varchar(50)", "SÍ (Clave Natural)");
                dgvMapeo.Rows.Add("SECTOR", "Sector", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("BARRIO / URB", "Barrio", "varchar(100)", "NO");
                dgvMapeo.Rows.Add("AREA_M2 / AREA", "AreaM2", "decimal(18,4)", "NO");
                dgvMapeo.Rows.Add("PERIMETRO", "PerimetroM", "decimal(18,4)", "NO");
                dgvMapeo.Rows.Add("[Geometría Polygon]", "Geometria (SRID 4326)", "geometry", "SÍ");
                break;

            case TipoCapa.Lotes:
                dgvMapeo.Rows.Add("COD_LOTE / LOTE", "CodigoLote", "varchar(50)", "SÍ (Clave Natural)");
                dgvMapeo.Rows.Add("COD_MZ / MZ", "CodigoManzana", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("NUM_PRED / PREDIO", "NumeroPredial", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("USO_SUELO / USO", "UsoSuelo", "varchar(100)", "NO");
                dgvMapeo.Rows.Add("TITULAR / PROPIETARIO", "Titular", "varchar(200)", "NO");
                dgvMapeo.Rows.Add("DIR / DIRECCION", "Direccion", "varchar(250)", "NO");
                dgvMapeo.Rows.Add("AREA / AREA_TERRENO", "AreaTerrenoM2", "decimal(18,4)", "NO");
                dgvMapeo.Rows.Add("FRENTE", "FrenteM", "decimal(18,4)", "NO");
                dgvMapeo.Rows.Add("FONDO", "FondoM", "decimal(18,4)", "NO");
                dgvMapeo.Rows.Add("[Geometría Polygon]", "Geometria (SRID 4326)", "geometry", "SÍ");
                break;

            case TipoCapa.CodigosFijos:
                dgvMapeo.Rows.Add("COD_FIJO / PUNTO", "CodigoFijo", "varchar(50)", "SÍ (Clave Natural)");
                dgvMapeo.Rows.Add("TIPO_PUNTO / TIPO", "TipoPunto", "varchar(100)", "NO");
                dgvMapeo.Rows.Add("DESC / NOMBRE", "Descripcion", "varchar(250)", "NO");
                dgvMapeo.Rows.Add("REF / REFERENCIA", "Referencia", "varchar(250)", "NO");
                dgvMapeo.Rows.Add("COTA_Z / COTA", "CotaZ", "decimal(10,4)", "NO");
                dgvMapeo.Rows.Add("[Geometría Point]", "Geometria (SRID 4326)", "geometry", "SÍ");
                break;

            case TipoCapa.Vias:
                dgvMapeo.Rows.Add("COD_VIA / VIA", "CodigoVia", "varchar(50)", "SÍ (Clave Natural)");
                dgvMapeo.Rows.Add("NOM_VIA / NOMBRE", "Nombre", "varchar(150)", "SÍ");
                dgvMapeo.Rows.Add("TIPO_VIA / TIPO", "TipoVia", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("JERARQUIA / NIVEL", "Jerarquia", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("SENTIDO", "Sentido", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("ANCHO_VIA / ANCHO", "AnchoViaM", "decimal(10,2)", "NO");
                dgvMapeo.Rows.Add("PAVIMENTO", "Pavimento", "varchar(50)", "NO");
                dgvMapeo.Rows.Add("LONGITUD", "LongitudM", "decimal(18,4)", "NO");
                dgvMapeo.Rows.Add("[Geometría LineString]", "Geometria (SRID 4326)", "geometry", "SÍ");
                break;

            default:
                foreach (var f in capa.CamposDbf)
                {
                    dgvMapeo.Rows.Add(f, f, "varchar(255)", "NO");
                }
                break;
        }
    }

    private async Task IniciarMigracionAsync()
    {
        if (_capasDetectadas.Count == 0)
        {
            MessageBox.Show("No se han detectado capas Shapefiles para migrar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ActualizarCadenaConexion();

        btnIniciarMigracion.Enabled = false;
        btnCancelarMigracion.Enabled = true;
        _cts = new CancellationTokenSource();

        pbProgreso.Value = 0;
        lblProgresoPorcentaje.Text = "0%";
        lblTotalStats.Text = "0";
        lblExitososStats.Text = "0";
        lblOmitidosStats.Text = "0";
        lblFallidosStats.Text = "0";
        lblTiempoStats.Text = "0 ms";

        int totalGlobal = 0, exitososGlobal = 0, omitidosGlobal = 0, fallidosGlobal = 0;

        try
        {
            var stopwatchTotal = System.Diagnostics.Stopwatch.StartNew();
            var capasValidas = _capasDetectadas.Where(c => c.EsValidoParaCarga || c.CapaEnum != null).ToList();
            int totalCapas = Math.Max(1, capasValidas.Count);
            int capaActualIndex = 0;

            foreach (var capa in _capasDetectadas)
            {
                if (_cts.IsCancellationRequested) break;

                if (!capa.EsValidoParaCarga && capa.CapaEnum == null)
                {
                    Log($"[OMITIDA] Capa '{capa.NombreCapaDetectada}' omitida por no ser un tipo reconocido o no ser válida.");
                    continue;
                }

                int indexParaProgreso = capaActualIndex;
                var tipoCapa = capa.CapaEnum ?? TipoCapa.Manzanas;
                Log($"[INICIO] Iniciando migración de capa '{tipoCapa}' desde '{capa.RutaShp}' (Modalidad: {(rbReemplazar.Checked ? "Reemplazar" : "Anexar")})...");

                var req = new IniciarMigracionRequestDto
                {
                    RutaCarpetaOShapefile = capa.RutaShp,
                    Capa = tipoCapa,
                    Modalidad = rbReemplazar.Checked ? ModalidadMigracion.Reemplazar : ModalidadMigracion.Anexar,
                    Usuario = string.IsNullOrWhiteSpace(txtUsuarioMigrador.Text) ? "admin" : txtUsuarioMigrador.Text.Trim()
                };

                var progress = new Progress<ProgresoMigracionDto>(p =>
                {
                    double subPorcentaje = p.Porcentaje;
                    double porcentajeGlobal = ((indexParaProgreso * 100.0) + subPorcentaje) / totalCapas;
                    porcentajeGlobal = Math.Min(100.0, Math.Max(0.0, porcentajeGlobal));

                    pbProgreso.Value = (int)Math.Round(porcentajeGlobal);
                    lblProgresoPorcentaje.Text = $"{porcentajeGlobal:F0}%";
                    lblEstadoOperacion.Text = $"[Capa {indexParaProgreso + 1}/{totalCapas}] {p.Capa}: {p.MensajeEstado} ({p.Procesados}/{p.TotalRegistros})";

                    lblTotalStats.Text = (totalGlobal + p.TotalRegistros).ToString();
                    lblExitososStats.Text = (exitososGlobal + p.Exitosos).ToString();
                    lblOmitidosStats.Text = (omitidosGlobal + p.Omitidos).ToString();
                    lblFallidosStats.Text = (fallidosGlobal + p.Fallidos).ToString();
                    lblTiempoStats.Text = $"{stopwatchTotal.ElapsedMilliseconds} ms";
                });

                var res = await _migradorService.EjecutarMigracionAsync(req, progress, _cts.Token);

                totalGlobal += res.TotalRegistros;
                exitososGlobal += res.RegistrosExitosos;
                omitidosGlobal += res.RegistrosOmitidos;
                fallidosGlobal += res.RegistrosFallidos;
                capaActualIndex++;

                Log($"[FIN] Capa '{tipoCapa}' finalizada. Estado: {res.Estado}, Exitosos: {res.RegistrosExitosos}, Omitidos: {res.RegistrosOmitidos}, Fallidos: {res.RegistrosFallidos}, Duración: {res.DuracionMs} ms.");

                if (res.Errores.Count > 0)
                {
                    foreach (var err in res.Errores) Log($"  ✖ Error: {err}");
                }
            }

            stopwatchTotal.Stop();
            lblTiempoStats.Text = $"{stopwatchTotal.ElapsedMilliseconds} ms";
            pbProgreso.Value = 100;
            lblProgresoPorcentaje.Text = "100%";

            lblTotalStats.Text = totalGlobal.ToString();
            lblExitososStats.Text = exitososGlobal.ToString();
            lblOmitidosStats.Text = omitidosGlobal.ToString();
            lblFallidosStats.Text = fallidosGlobal.ToString();

            if (_cts.IsCancellationRequested)
            {
                lblEstadoOperacion.Text = "Proceso cancelado por el usuario.";
                Log("Proceso de migración cancelado por el usuario.");
            }
            else
            {
                lblEstadoOperacion.Text = "✔ Proceso de migración completado exitosamente.";
                Log($"Proceso general completado. Total persistido: {exitososGlobal} registros con SRID 4326.");
                MessageBox.Show($"Migración completada con éxito.\n\nTotal procesados: {totalGlobal}\nExitosos: {exitososGlobal}\nOmitidos: {omitidosGlobal}\nFallidos: {fallidosGlobal}", "Migración Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            await CargarHistorialAsync();
        }
        catch (Exception ex)
        {
            lblEstadoOperacion.Text = "✖ Error durante la migración: " + ex.Message;
            Log($"Error crítico durante la migración: {ex.Message}");
            MessageBox.Show($"Ocurrió un error crítico durante la migración:\n\n{ex.Message}", "Error de Migración", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnIniciarMigracion.Enabled = true;
            btnCancelarMigracion.Enabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelarMigracion()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            btnCancelarMigracion.Enabled = false;
            lblEstadoOperacion.Text = "Cancelando operación y revirtiendo transacciones pendientes...";
            Log("Solicitud de cancelación enviada.");
        }
    }

    private async Task CargarHistorialAsync()
    {
        try
        {
            dgvHistorial.Rows.Clear();
            var historial = await _migradorService.ObtenerHistorialMigracionesAsync();
            foreach (var h in historial)
            {
                dgvHistorial.Rows.Add(
                    h.IdMigracion,
                    h.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss"),
                    h.Usuario,
                    h.Capa,
                    h.NombreArchivo,
                    h.Modalidad,
                    h.TotalRegistros,
                    h.RegistrosExitosos,
                    h.RegistrosOmitidos,
                    h.RegistrosFallidos,
                    h.Estado,
                    h.DuracionMs
                );
            }
            Log($"Historial actualizado ({historial.Count} registros recuperados de la base de datos).");
        }
        catch (Exception ex)
        {
            Log($"Error al cargar historial: {ex.Message}");
        }
    }

    private void ExportarLog()
    {
        using var sfd = new SaveFileDialog();
        sfd.Filter = "Archivo de Texto (*.txt)|*.txt|Archivo CSV (*.csv)|*.csv";
        sfd.FileName = $"Bitacora_Migracion_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            var lines = lstLog.Items.Cast<string>();
            File.WriteAllLines(sfd.FileName, lines);
            MessageBox.Show($"Bitácora exportada exitosamente a:\n\n{sfd.FileName}", "Exportación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void Log(string mensaje)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var entry = $"[{timestamp}] {mensaje}";
        if (lstLog.InvokeRequired)
        {
            lstLog.Invoke(new Action(() => { lstLog.Items.Add(entry); lstLog.TopIndex = lstLog.Items.Count - 1; }));
        }
        else
        {
            lstLog.Items.Add(entry);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
    }
    #endregion
}
