#nullable enable
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Infrastructure.Data;
using VisorDatosSIG.Infrastructure.Repositories;
using VisorDatosSIG.Infrastructure.Shapefiles;

try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// 1. Configuración de MVC, Compresión HTTP y Vistas Razor
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configuración CORS para App Móvil Flutter y pruebas en red WiFi
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsMovil", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. Configuración de Autenticación por Cookies (RBAC: Administrador y Consultor)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Home/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "VisorDatosSIG.AuthSession";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("ConsultorOAdmin", policy => policy.RequireRole("Administrador", "Consultor"));
});

// 3. Inyección de Dependencias de Infraestructura y Aplicación
var connectionString = builder.Configuration.GetConnectionString("VisorDatosSIG_BD")
    ?? "Server=.\\SQLEXPRESS;Database=VisorDatosSIG_BD;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";

builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

builder.Services.AddScoped<ICapaRepository, CapaRepository>();
builder.Services.AddScoped<IBusquedaRepository, BusquedaRepository>();
builder.Services.AddScoped<IMigracionRepository, MigracionRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IBitacoraRepository, BitacoraRepository>();
builder.Services.AddScoped<IAguaRepository, AguaRepository>();

builder.Services.AddSingleton<IShapefileReader, ShapefileReader>();
builder.Services.AddScoped<ICapaService, CapaService>();
builder.Services.AddScoped<IBusquedaService, BusquedaService>();
builder.Services.AddScoped<IMigradorService, MigradorService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IDijkstraRoutingService, DijkstraRoutingService>();
builder.Services.AddScoped<IAguaService, AguaService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("CorsMovil");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "login",
    pattern: "Login",
    defaults: new { controller = "Home", action = "Login" });

app.MapControllerRoute(
    name: "logout",
    pattern: "Logout",
    defaults: new { controller = "Home", action = "Logout" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

