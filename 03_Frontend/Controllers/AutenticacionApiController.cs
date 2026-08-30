#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Seguridad;
using VisorDatosSIG.Application.Exceptions;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Web.Controllers;

[ApiController]
[Route("api/autenticacion")]
public class AutenticacionApiController : ControllerBase
{
    private readonly IAuthService _authService;

    public AutenticacionApiController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new LoginResponseDto
            {
                Exitoso = false,
                Mensaje = "Debe proporcionar usuario y contraseña."
            });
        }

        var ipCliente = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var resultado = await _authService.LoginAsync(request, ipCliente);

        if (resultado.Exitoso && resultado.Usuario != null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, resultado.Usuario.IdUsuario.ToString()),
                new(ClaimTypes.Name, resultado.Usuario.Username),
                new(ClaimTypes.GivenName, resultado.Usuario.NombreCompleto ?? resultado.Usuario.Username),
                new(ClaimTypes.Email, resultado.Usuario.Email ?? string.Empty),
                new(ClaimTypes.Role, resultado.Usuario.Rol),
                new("IdRol", resultado.Usuario.IdRol.ToString()),
                new("Token", resultado.Token ?? string.Empty)
            };

            // Inyectar Claims de Permisos Granulares
            if (resultado.Usuario.Permisos != null)
            {
                foreach (var permiso in resultado.Usuario.Permisos)
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
        }

        return Ok(resultado);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { exitoso = true, mensaje = "Sesión finalizada correctamente." });
    }

    [HttpGet("usuario-actual")]
    public IActionResult ObtenerUsuarioActual()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new { autenticado = false });
        }

        var permisos = User.FindAll("Permiso").Select(c => c.Value).ToList();

        return Ok(new
        {
            autenticado = true,
            idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier),
            username = User.Identity.Name,
            nombreCompleto = User.FindFirstValue(ClaimTypes.GivenName),
            email = User.FindFirstValue(ClaimTypes.Email),
            rol = User.FindFirstValue(ClaimTypes.Role),
            idRol = User.FindFirstValue("IdRol"),
            esAdmin = User.IsInRole("Administrador"),
            permisos = permisos
        });
    }

    [HttpGet("permisos/catalogo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerCatalogoPermisos()
    {
        var catalogo = await _authService.ObtenerCatalogoPermisosAgrupadoAsync();
        return Ok(catalogo);
    }

    [HttpGet("usuarios/{id:int}/permisos")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerPermisosUsuario(int id)
    {
        var permisos = await _authService.ObtenerPermisosUsuarioAsync(id);
        return Ok(permisos);
    }

    [HttpPost("usuarios")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto request)
    {
        try
        {
            var adminUsername = User.Identity?.Name ?? "admin";
            var nuevo = await _authService.CrearUsuarioAsync(request, adminUsername);
            return Ok(new { exitoso = true, mensaje = $"Usuario '{nuevo.Username}' creado exitosamente.", usuario = nuevo });
        }
        catch (ValidacionException ex)
        {
            return BadRequest(new { exitoso = false, mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { exitoso = false, mensaje = "Error al crear usuario: " + ex.Message });
        }
    }

    [HttpPut("usuarios")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ActualizarUsuario([FromBody] ActualizarUsuarioDto request)
    {
        try
        {
            var adminUsername = User.Identity?.Name ?? "admin";
            var ok = await _authService.ActualizarUsuarioCompletoAsync(request, adminUsername);
            return Ok(new { exitoso = ok, mensaje = ok ? "Usuario actualizado correctamente." : "No se pudo actualizar." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { exitoso = false, mensaje = ex.Message });
        }
    }

    [HttpPut("usuarios/{id:int}/permisos")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ActualizarPermisos(int id, [FromBody] ActualizarPermisosDto request)
    {
        try
        {
            var adminUsername = User.Identity?.Name ?? "admin";
            var ok = await _authService.ActualizarPermisosUsuarioAsync(id, request.Permisos, adminUsername);
            return Ok(new { exitoso = ok, mensaje = ok ? "Permisos actualizados correctamente." : "No se pudieron actualizar los permisos." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { exitoso = false, mensaje = ex.Message });
        }
    }

    [HttpPost("usuarios/{id:int}/toggle-estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ToggleEstadoUsuario(int id)
    {
        var adminUsername = User.Identity?.Name ?? "admin";
        var ok = await _authService.ToggleEstadoUsuarioAsync(id, adminUsername);
        return Ok(new { exitoso = ok, mensaje = ok ? "Estado de cuenta modificado correctamente." : "No se pudo cambiar el estado." });
    }

    [HttpPost("usuarios/{id:int}/desbloquear")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DesbloquearUsuario(int id)
    {
        var adminUsername = User.Identity?.Name ?? "admin";
        var exitoso = await _authService.DesbloquearUsuarioAsync(id, adminUsername);

        if (exitoso)
        {
            return Ok(new { exitoso = true, mensaje = "Usuario desbloqueado correctamente." });
        }

        return BadRequest(new { exitoso = false, mensaje = "No se pudo desbloquear al usuario." });
    }

    [HttpDelete("usuarios/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == id.ToString())
        {
            return BadRequest(new { exitoso = false, mensaje = "No puedes eliminar tu propia cuenta de administrador activa." });
        }

        var adminUsername = User.Identity?.Name ?? "admin";
        var ok = await _authService.EliminarUsuarioAsync(id, adminUsername);
        return Ok(new { exitoso = ok, mensaje = ok ? "Usuario eliminado correctamente." : "No se pudo eliminar el usuario." });
    }
}
