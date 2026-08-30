#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Seguridad;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticacionController : ControllerBase
{
    private readonly IAuthService _authService;

    public AutenticacionController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Inicia sesión validando credenciales con hash PBKDF2-SHA512 y control de bloqueo tras 5 intentos fallidos.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ipCliente = HttpContext.Connection.RemoteIpAddress?.ToString();
        var respuesta = await _authService.LoginAsync(request, ipCliente);

        if (!respuesta.Exitoso)
        {
            return BadRequest(respuesta);
        }

        return Ok(respuesta);
    }

    /// <summary>
    /// Lista todos los usuarios registrados (requiere rol de Administrador).
    /// </summary>
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(List<UsuarioDto>), 200)]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        var usuarios = await _authService.ObtenerUsuariosAsync();
        return Ok(usuarios);
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema.
    /// </summary>
    [HttpPost("usuarios")]
    [ProducesResponseType(typeof(UsuarioDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto request)
    {
        var adminUser = User.Identity?.Name ?? "admin";
        var creado = await _authService.CrearUsuarioAsync(request, adminUser);
        return CreatedAtAction(nameof(ObtenerUsuarios), new { id = creado.IdUsuario }, creado);
    }

    /// <summary>
    /// Permite el cambio de contraseña de un usuario validando su clave actual.
    /// </summary>
    [HttpPost("cambiar-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto request)
    {
        var exito = await _authService.CambiarPasswordAsync(request);
        if (!exito)
        {
            return BadRequest(new { exitoso = false, mensaje = "No se pudo actualizar la contraseña." });
        }

        return Ok(new { exitoso = true, mensaje = "Contraseña modificada correctamente." });
    }

    /// <summary>
    /// Desbloquea un usuario bloqueado por intentos fallidos de acceso.
    /// </summary>
    [HttpPost("usuarios/{id:int}/desbloquear")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DesbloquearUsuario([FromRoute] int id)
    {
        var adminUser = User.Identity?.Name ?? "admin";
        var exito = await _authService.DesbloquearUsuarioAsync(id, adminUser);
        if (!exito)
        {
            return NotFound(new { exitoso = false, mensaje = "Usuario no encontrado." });
        }

        return Ok(new { exitoso = true, mensaje = "Usuario desbloqueado correctamente." });
    }
}
