#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.DTOs.Auditoria;
using VisorDatosSIG.Application.Interfaces.Services;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _auditoriaService;

    public AuditoriaController(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Consulta los eventos registrados en la bitácora de auditoría con filtros por fecha, tipo de evento y usuario.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BitacoraPaginadaDto), 200)]
    public async Task<IActionResult> ObtenerBitacora(
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] string? tipoEvento = null,
        [FromQuery] string? usuario = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int registrosPorPagina = 50)
    {
        var filtro = new BitacoraFiltroDto
        {
            FechaDesde = desde,
            FechaHasta = hasta,
            TipoEvento = tipoEvento,
            Usuario = usuario,
            Pagina = Math.Max(1, pagina),
            RegistrosPorPagina = Math.Clamp(registrosPorPagina, 1, 200)
        };

        var resultado = await _auditoriaService.ObtenerBitacoraAsync(filtro);
        return Ok(resultado);
    }
}
