using VisorDatosSIG.Application.DTOs.Auditoria;
using VisorDatosSIG.Application.DTOs.Busqueda;
using VisorDatosSIG.Application.DTOs.Capas;
using VisorDatosSIG.Application.DTOs.GeoJson;
using VisorDatosSIG.Application.DTOs.Migracion;
using VisorDatosSIG.Application.DTOs.Seguridad;
using VisorDatosSIG.Domain.Enums;
using VisorDatosSIG.Domain.ValueObjects;

namespace VisorDatosSIG.Application.Interfaces.Services;

public interface ICapaService
{
    Task<List<CapaCatalogoDto>> ObtenerCatalogoCapasAsync();
    Task<GeoJsonFeatureCollection> ObtenerGeoJsonCapaAsync(string nombreCapa, BoundingBox? bbox = null, int limite = 5000);
    Task<EntidadDetalleDto?> ObtenerDetalleEntidadAsync(string nombreCapa, int idEntidad);
    void InvalidarCache(string? nombreCapa = null);
}

public interface IBusquedaService
{
    Task<BusquedaResultadoPaginadoDto> BuscarAsync(BusquedaRequestDto request);
    Task<List<IdentificacionItemDto>> IdentificarPorPuntoAsync(IdentificacionPuntoRequestDto request);
}

public interface IMigradorService
{
    Task<ValidacionShapefileDto> ValidarShapefileAsync(string rutaShp);
    Task<List<VistaPreviaRegistroDto>> ObtenerVistaPreviaAsync(string rutaShp, int cantidadFilas = 20);
    Task<ResumenMigracionDto> EjecutarMigracionAsync(
        IniciarMigracionRequestDto request, 
        IProgress<ProgresoMigracionDto>? progreso = null, 
        CancellationToken cancellationToken = default);
    Task<List<ResumenMigracionDto>> ObtenerHistorialMigracionesAsync();
}

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipCliente = null);
    Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int idUsuario);
    Task<List<UsuarioDto>> ObtenerUsuariosAsync();
    Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto request, string usuarioAdmin);
    Task<bool> ActualizarUsuarioCompletoAsync(ActualizarUsuarioDto request, string usuarioAdmin);
    Task<bool> CambiarPasswordAsync(CambiarPasswordDto request);
    Task<bool> DesbloquearUsuarioAsync(int idUsuario, string usuarioAdmin);
    Task<bool> ToggleEstadoUsuarioAsync(int idUsuario, string usuarioAdmin);
    Task<bool> EliminarUsuarioAsync(int idUsuario, string usuarioAdmin);
    Task<List<GrupoPermisosDto>> ObtenerCatalogoPermisosAgrupadoAsync();
    Task<List<string>> ObtenerPermisosUsuarioAsync(int idUsuario);
    Task<bool> ActualizarPermisosUsuarioAsync(int idUsuario, List<string> permisos, string usuarioAdmin);
}

public interface IAuditoriaService
{
    Task RegistrarEventoAsync(string tipoEvento, string modulo, string descripcion, string? usuario = null, string? ipCliente = null, string? detalleTecnico = null, bool exitoso = true);
    Task<BitacoraPaginadaDto> ObtenerBitacoraAsync(BitacoraFiltroDto filtro);
}

public interface IShapefileReader
{
    ValidacionShapefileDto InspeccionarShapefile(string rutaShp);
    List<VistaPreviaRegistroDto> LeerVistaPrevia(string rutaShp, int cantidad = 20);
    IEnumerable<(string OrigenId, string Wkt, Dictionary<string, object?> Atributos)> LeerRegistrosCompletos(string rutaShp);
}
