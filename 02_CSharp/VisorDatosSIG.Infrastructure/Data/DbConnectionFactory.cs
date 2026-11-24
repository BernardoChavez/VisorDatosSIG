#nullable enable
using System;
using System.Data;
using System.Data.SqlClient;

namespace VisorDatosSIG.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CrearConexion();
    Task<IDbConnection> CrearConexionAbiertaAsync(CancellationToken cancellationToken = default);
    string CadenaConexion { get; }
    void ConfigurarCadenaConexion(string nuevaCadena);
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private string _cadenaConexion;

    public string CadenaConexion => _cadenaConexion;

    public DbConnectionFactory() : this(null)
    {
    }

    public DbConnectionFactory(string? cadenaConexion)
    {
        // 1. Prioridad: Cadena suministrada
        // 2. Prioridad: Variable de entorno
        // 3. Prioridad: Default SQL Server local (Docker en localhost,1433)
        _cadenaConexion = cadenaConexion 
            ?? Environment.GetEnvironmentVariable("SQLCONNSTR_VisorDatosSIG_BD")
            ?? "Server=localhost,1433;Database=VisorDatosSIG_BD;User Id=sa;Password=SqlAdmin2026*!;TrustServerCertificate=True;Connect Timeout=30;";
    }

    public void ConfigurarCadenaConexion(string nuevaCadena)
    {
        if (string.IsNullOrWhiteSpace(nuevaCadena))
            throw new ArgumentException("La cadena de conexión no puede ser nula ni vacía.", nameof(nuevaCadena));

        _cadenaConexion = nuevaCadena;
    }

    public IDbConnection CrearConexion()
    {
        return new SqlConnection(_cadenaConexion);
    }

    public async Task<IDbConnection> CrearConexionAbiertaAsync(CancellationToken cancellationToken = default)
    {
        var conn = new SqlConnection(_cadenaConexion);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }

    public static string ConstruirCadena(string servidor, string baseDatos, bool seguridadIntegrada, string? usuario = null, string? password = null)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = servidor,
            InitialCatalog = baseDatos,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        if (seguridadIntegrada)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = usuario ?? "";
            builder.Password = password ?? "";
        }

        return builder.ConnectionString;
    }
}
