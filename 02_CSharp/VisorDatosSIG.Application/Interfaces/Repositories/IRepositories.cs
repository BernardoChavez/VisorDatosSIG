using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.Enums;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Application.Interfaces.Repositories;

public interface ICapaRepository
{
    Task<List<CatalogoCapa>> ObtenerCatalogoCapasAsync();
    Task<CatalogoCapa?> ObtenerCapaPorNombreAsync(string nombreCapa);
    Task<List<Manzana>> ObtenerManzanasPorBBoxAsync(BoundingBox? bbox, int limite = 5000);
    Task<List<Lote>> ObtenerLotesPorBBoxAsync(BoundingBox? bbox, int limite = 5000);
    Task<List<CodigoFijo>> ObtenerCodigosFijosPorBBoxAsync(BoundingBox? bbox, int limite = 5000);
    Task<List<Via>> ObtenerViasPorBBoxAsync(BoundingBox? bbox, int limite = 5000);
    Task<object?> ObtenerEntidadPorIdAsync(string nombreCapa, int idEntidad);
}

public interface IBusquedaRepository
{
    Task<(int Total, List<object> Resultados)> BuscarAsync(string termino, string? capa, int pagina, int registrosPorPagina);
    Task<List<object>> IdentificarPorPuntoAsync(double lon, double lat, double toleranciaMetros = 5.0);
}

public interface IMigracionRepository
{
    Task<int> IniciarMigracionAsync(string usuario, string capa, string nombreArchivo, string rutaArchivo, string modalidad, int totalRegistros);
    Task ActualizarEstadoMigracionAsync(int idMigracion, int exitosos, int omitidos, int fallidos, string estado, string? detalle = null);
    Task<List<Migracion>> ObtenerHistorialMigracionesAsync(int limite = 50);
    Task<int> InsertarManzanasPorLoteAsync(List<Manzana> manzanas, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null);
    Task<int> InsertarLotesPorLoteAsync(List<Lote> lotes, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null);
    Task<int> InsertarCodigosFijosPorLoteAsync(List<CodigoFijo> codigosFijos, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null);
    Task<int> InsertarViasPorLoteAsync(List<Via> vias, int idMigracion, bool reemplazar, CancellationToken cancellationToken, Action<int, int>? progreso = null);
}

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorUsernameAsync(string username);
    Task<Usuario?> ObtenerPorIdAsync(int idUsuario);
    Task<List<Usuario>> ObtenerTodosAsync();
    Task<int> CrearUsuarioAsync(Usuario usuario);
    Task<bool> ActualizarUsuarioAsync(Usuario usuario);
    Task<bool> ActualizarPasswordAsync(int idUsuario, string nuevoHash, string nuevoSalt);
    Task RegistrarIntentoLoginAsync(string username, bool exitoso);
    Task<List<string>> ObtenerPermisosUsuarioAsync(int idUsuario);
    Task<bool> GuardarUsuarioPermisosAsync(int idUsuario, List<string> permisos);
    Task<List<Permiso>> ObtenerCatalogoPermisosAsync();
    Task<bool> EliminarUsuarioAsync(int idUsuario);
}

public interface IBitacoraRepository
{
    Task RegistrarEventoAsync(BitacoraEvento evento);
    Task<(int Total, List<BitacoraEvento> Items)> ObtenerBitacoraAsync(DateTime? desde, DateTime? hasta, string? tipoEvento, string? usuario, int pagina, int registrosPorPagina);
}
