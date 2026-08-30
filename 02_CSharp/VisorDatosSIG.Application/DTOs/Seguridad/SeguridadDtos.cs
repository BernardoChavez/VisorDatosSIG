#nullable enable
using System;
using System.Collections.Generic;

namespace VisorDatosSIG.Application.DTOs.Seguridad;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Exitoso { get; set; }
    public string? Mensaje { get; set; }
    public string? Token { get; set; }
    public UsuarioDto? Usuario { get; set; }
}

public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public List<string> Permisos { get; set; } = new();
}

public class CrearUsuarioDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IdRol { get; set; } = 2; // Consultor por defecto
    public List<string> Permisos { get; set; } = new();
}

public class ActualizarUsuarioDto
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public bool Activo { get; set; } = true;
    public string? PasswordNuevo { get; set; }
    public List<string> Permisos { get; set; } = new();
}

public class CambiarPasswordDto
{
    public int IdUsuario { get; set; }
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNuevo { get; set; } = string.Empty;
}

public class PermisoDto
{
    public string CodigoPermiso { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
}

public class GrupoPermisosDto
{
    public string Categoria { get; set; } = string.Empty;
    public List<PermisoDto> Permisos { get; set; } = new();
}

public class ActualizarPermisosDto
{
    public List<string> Permisos { get; set; } = new();
}
