#nullable enable
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VisorDatosSIG.Api.Middlewares;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Infrastructure.Data;
using VisorDatosSIG.Infrastructure.Repositories;
using VisorDatosSIG.Infrastructure.Shapefiles;

try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Controladores y Serialización JSON estándar GeoJSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 2. Configuración de CORS para el Visor Web (Leaflet / MVC / SPA)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Inyección de Dependencias (Clean Architecture)
var connectionString = builder.Configuration.GetConnectionString("VisorDatosSIG_BD")
    ?? "Server=.\\SQLEXPRESS;Database=VisorDatosSIG_BD;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";

builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

// Repositorios de Infraestructura
builder.Services.AddScoped<ICapaRepository, CapaRepository>();
builder.Services.AddScoped<IBusquedaRepository, BusquedaRepository>();
builder.Services.AddScoped<IMigracionRepository, MigracionRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IBitacoraRepository, BitacoraRepository>();
builder.Services.AddScoped<IAguaRepository, AguaRepository>();

// Servicios de Dominio / Aplicación
builder.Services.AddSingleton<IShapefileReader, ShapefileReader>();
builder.Services.AddScoped<ICapaService, CapaService>();
builder.Services.AddScoped<IBusquedaService, BusquedaService>();
builder.Services.AddScoped<IMigradorService, MigradorService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IDijkstraRoutingService, DijkstraRoutingService>();
builder.Services.AddScoped<IAguaService, AguaService>();

builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// 4. OpenAPI / Endpoints Explorer
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Pipeline de Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseResponseCompression();
app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

// Endpoint de Documentación / Sitemap de la API
app.MapGet("/api", () => Results.Ok(new
{
    api = "VisorDatosSIG RESTful Web API",
    version = "2026.1.0",
    especificacion = "WGS84 (SRID 4326) + RFC 7946 GeoJSON",
    endpoints = new[]
    {
        "GET  /api/health",
        "GET  /api/capas",
        "GET  /api/capas/{nombreCapa}/geojson?bbox={minLon,minLat,maxLon,maxLat}",
        "GET  /api/capas/{nombreCapa}/{id}",
        "GET  /api/busqueda?q={termino}&capa={capa}&pagina={1}",
        "GET  /api/busqueda/identificar?lon={lon}&lat={lat}&tolerancia={5}",
        "POST /api/autenticacion/login",
        "GET  /api/autenticacion/usuarios",
        "GET  /api/migraciones/historial",
        "GET  /api/auditoria"
    }
}));

// Endpoint de Health Check
app.MapGet("/api/health", async (IDbConnectionFactory dbFactory) =>
{
    var bdConectada = false;
    string? errorBd = null;
    try
    {
        using var conn = await dbFactory.CrearConexionAbiertaAsync();
        bdConectada = true;
    }
    catch (Exception ex)
    {
        errorBd = ex.Message;
    }

    return Results.Ok(new
    {
        sistema = "VisorDatosSIG API",
        version = "2026.1.0",
        estado = bdConectada ? "Saludable" : "Degradado",
        baseDatosConectada = bdConectada,
        errorBaseDatos = errorBd,
        timestamp = DateTime.UtcNow
    });
});

app.MapControllers();

app.Run();
