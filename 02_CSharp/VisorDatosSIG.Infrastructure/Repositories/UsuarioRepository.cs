#nullable enable
using System.Data;
using System.Data.SqlClient;
using VisorDatosSIG.Application.Interfaces.Repositories;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Infrastructure.Data;

namespace VisorDatosSIG.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public UsuarioRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Usuario?> ObtenerPorUsernameAsync(string username)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerUsuarioPorUsername", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Username", username);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Salt = reader.GetString(reader.GetOrdinal("Salt")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                Rol = new Rol
                {
                    IdRol = reader.GetInt32(reader.GetOrdinal("IdRol")),
                    Nombre = reader.GetString(reader.GetOrdinal("Rol"))
                },
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                IntentosFallidos = reader.GetInt32(reader.GetOrdinal("IntentosFallidos")),
                BloqueadoHasta = reader.IsDBNull(reader.GetOrdinal("BloqueadoHasta")) ? null : reader.GetDateTime(reader.GetOrdinal("BloqueadoHasta")),
                UltimoAcceso = reader.IsDBNull(reader.GetOrdinal("UltimoAcceso")) ? null : reader.GetDateTime(reader.GetOrdinal("UltimoAcceso"))
            };
        }

        return null;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        var sql = @"SELECT u.IdUsuario, u.Username, u.PasswordHash, u.Salt, u.NombreCompleto, u.Email, u.IdRol, r.Nombre AS Rol, u.Activo, u.IntentosFallidos, u.BloqueadoHasta, u.UltimoAcceso, u.FechaCreacion
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.Roles r ON u.IdRol = r.IdRol
                    WHERE u.IdUsuario = @Id";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", idUsuario);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Salt = reader.GetString(3),
                NombreCompleto = reader.GetString(4),
                Email = reader.GetString(5),
                IdRol = reader.GetInt32(6),
                Rol = new Rol { IdRol = reader.GetInt32(6), Nombre = reader.GetString(7) },
                Activo = reader.GetBoolean(8),
                IntentosFallidos = reader.GetInt32(9),
                BloqueadoHasta = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                UltimoAcceso = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                FechaCreacion = reader.GetDateTime(12)
            };
        }

        return null;
    }

    public async Task<List<Usuario>> ObtenerTodosAsync()
    {
        var lista = new List<Usuario>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        var sql = @"SELECT u.IdUsuario, u.Username, u.PasswordHash, u.Salt, u.NombreCompleto, u.Email, u.IdRol, r.Nombre AS Rol, u.Activo, u.IntentosFallidos, u.BloqueadoHasta, u.UltimoAcceso, u.FechaCreacion
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.Roles r ON u.IdRol = r.IdRol
                    ORDER BY u.Username ASC";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Usuario
            {
                IdUsuario = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Salt = reader.GetString(3),
                NombreCompleto = reader.GetString(4),
                Email = reader.GetString(5),
                IdRol = reader.GetInt32(6),
                Rol = new Rol { IdRol = reader.GetInt32(6), Nombre = reader.GetString(7) },
                Activo = reader.GetBoolean(8),
                IntentosFallidos = reader.GetInt32(9),
                BloqueadoHasta = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                UltimoAcceso = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                FechaCreacion = reader.GetDateTime(12)
            });
        }

        return lista;
    }

    public async Task<int> CrearUsuarioAsync(Usuario usuario)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        var sql = @"INSERT INTO dbo.Usuarios (Username, PasswordHash, Salt, NombreCompleto, Email, IdRol, Activo, IntentosFallidos, FechaCreacion)
                    VALUES (@Username, @PasswordHash, @Salt, @NombreCompleto, @Email, @IdRol, @Activo, 0, SYSUTCDATETIME());
                    SELECT SCOPE_IDENTITY();";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", usuario.Username);
        cmd.Parameters.AddWithValue("@PasswordHash", usuario.PasswordHash);
        cmd.Parameters.AddWithValue("@Salt", usuario.Salt);
        cmd.Parameters.AddWithValue("@NombreCompleto", usuario.NombreCompleto);
        cmd.Parameters.AddWithValue("@Email", usuario.Email);
        cmd.Parameters.AddWithValue("@IdRol", usuario.IdRol);
        cmd.Parameters.AddWithValue("@Activo", usuario.Activo);

        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<bool> ActualizarUsuarioAsync(Usuario usuario)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        var sql = @"UPDATE dbo.Usuarios 
                    SET NombreCompleto = @NombreCompleto, 
                        Email = @Email, 
                        IdRol = @IdRol, 
                        Activo = @Activo,
                        IntentosFallidos = @IntentosFallidos,
                        BloqueadoHasta = @BloqueadoHasta
                    WHERE IdUsuario = @IdUsuario";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
        cmd.Parameters.AddWithValue("@NombreCompleto", usuario.NombreCompleto);
        cmd.Parameters.AddWithValue("@Email", usuario.Email);
        cmd.Parameters.AddWithValue("@IdRol", usuario.IdRol);
        cmd.Parameters.AddWithValue("@Activo", usuario.Activo);
        cmd.Parameters.AddWithValue("@IntentosFallidos", usuario.IntentosFallidos);
        cmd.Parameters.AddWithValue("@BloqueadoHasta", (object?)usuario.BloqueadoHasta ?? DBNull.Value);

        var filas = await cmd.ExecuteNonQueryAsync();
        return filas > 0;
    }

    public async Task<bool> ActualizarPasswordAsync(int idUsuario, string nuevoHash, string nuevoSalt)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        var sql = @"UPDATE dbo.Usuarios 
                    SET PasswordHash = @PasswordHash, 
                        Salt = @Salt,
                        IntentosFallidos = 0,
                        BloqueadoHasta = NULL
                    WHERE IdUsuario = @IdUsuario";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
        cmd.Parameters.AddWithValue("@PasswordHash", nuevoHash);
        cmd.Parameters.AddWithValue("@Salt", nuevoSalt);

        var filas = await cmd.ExecuteNonQueryAsync();
        return filas > 0;
    }

    public async Task RegistrarIntentoLoginAsync(string username, bool exitoso)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_RegistrarIntentoLogin", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@Exitoso", exitoso ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> ObtenerPermisosUsuarioAsync(int idUsuario)
    {
        var permisos = new List<string>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerPermisosUsuario", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            permisos.Add(reader.GetString(0));
        }

        return permisos;
    }

    public async Task<bool> GuardarUsuarioPermisosAsync(int idUsuario, List<string> permisos)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_GuardarUsuarioPermisos", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
        cmd.Parameters.AddWithValue("@PermisosCsv", string.Join(",", permisos ?? new List<string>()));

        await cmd.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<Permiso>> ObtenerCatalogoPermisosAsync()
    {
        var lista = new List<Permiso>();
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_ObtenerCatalogoPermisos", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Permiso
            {
                CodigoPermiso = reader.GetString(reader.GetOrdinal("CodigoPermiso")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
                Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString(reader.GetOrdinal("Descripcion")),
                Orden = reader.GetInt32(reader.GetOrdinal("Orden"))
            });
        }

        return lista;
    }

    public async Task<bool> EliminarUsuarioAsync(int idUsuario)
    {
        using var conn = (SqlConnection)await _dbFactory.CrearConexionAbiertaAsync();
        using var cmd = new SqlCommand("dbo.usp_EliminarUsuario", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

        await cmd.ExecuteNonQueryAsync();
        return true;
    }
}

