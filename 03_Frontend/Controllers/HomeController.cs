#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Capas;
using VisorDatosSIG.Application.DTOs.Seguridad;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICapaService _capaService;
    private readonly IAuthService _authService;

    public HomeController(ICapaService capaService, IAuthService authService)
    {
        _capaService = capaService;
        _authService = authService;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var idUsuarioStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(idUsuarioStr, out int idUsuario);

        var permisosClaims = User.FindAll("Permiso").Select(c => c.Value).ToHashSet();

        // Si la sesión no tiene claims de permisos (ej. cookie anterior), consultamos permisos frescos
        if (permisosClaims.Count == 0 && idUsuario > 0)
        {
            var permisosDb = await _authService.ObtenerPermisosUsuarioAsync(idUsuario);
            foreach (var p in permisosDb) permisosClaims.Add(p);
        }

        var esAdmin = User.IsInRole("Administrador") || string.Equals(User.Identity?.Name, "admin", StringComparison.OrdinalIgnoreCase);

        // Si es Admin o no tiene permisos asignados en BD, conceder permisos completos de capas y visor por defecto
        if (esAdmin || permisosClaims.Count == 0)
        {
            permisosClaims.Add("CAPA_Manzanas");
            permisosClaims.Add("CAPA_Lotes");
            permisosClaims.Add("CAPA_CodigosFijos");
            permisosClaims.Add("CAPA_Vias");
            permisosClaims.Add("FUNC_BUSQUEDA_ESPACIAL");
            permisosClaims.Add("HERR_IDENTIFICAR");
            permisosClaims.Add("HERR_MEDICION_DISTANCIA");
            permisosClaims.Add("HERR_MEDICION_AREA");
            permisosClaims.Add("HERR_GPS");
            permisosClaims.Add("HERR_COORDENADAS");
            permisosClaims.Add("HERR_RESTABLECER_VISTA");
            permisosClaims.Add("FUNC_EXPORTAR_WKT");
            permisosClaims.Add("FUNC_CAMBIAR_MAPA_BASE");
            permisosClaims.Add("FUNC_CAMBIAR_TEMA");
        }

        var catalogoCapas = await _capaService.ObtenerCatalogoCapasAsync();

        // Filtrar catálogo de capas para enviar a la vista únicamente las que el usuario tiene autorizadas
        var capasPermitidas = catalogoCapas
            .Where(c => esAdmin || permisosClaims.Contains($"CAPA_{c.Nombre}"))
            .ToList();

        ViewBag.Capas = capasPermitidas;
        ViewBag.Permisos = permisosClaims.ToList();

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] LoginRequestDto model, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
        {
            ViewBag.Error = "Debe ingresar su usuario y contraseña.";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var ipCliente = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var res = await _authService.LoginAsync(model, ipCliente);

        if (!res.Exitoso || res.Usuario == null)
        {
            ViewBag.Error = res.Mensaje ?? "Credenciales inválidas o cuenta inactiva.";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, res.Usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, res.Usuario.Username),
            new(ClaimTypes.GivenName, res.Usuario.NombreCompleto ?? res.Usuario.Username),
            new(ClaimTypes.Email, res.Usuario.Email ?? string.Empty),
            new(ClaimTypes.Role, res.Usuario.Rol),
            new("IdRol", res.Usuario.IdRol.ToString()),
            new("Token", res.Token ?? string.Empty)
        };

        // Inyectar Claims de Permisos Granulares
        if (res.Usuario.Permisos != null)
        {
            foreach (var permiso in res.Usuario.Permisos)
            {
                claims.Add(new Claim("Permiso", permiso));
            }
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Home");
    }

    [HttpGet]
    [Authorize]
    public IActionResult AccesoDenegado()
    {
        return View();
    }
}
