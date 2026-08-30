#nullable enable
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VisorDatosSIG.Application.Exceptions;

namespace VisorDatosSIG.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada capturada en middleware: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = HttpStatusCode.InternalServerError;
        var mensaje = "Ha ocurrido un error interno en el servidor.";
        var tipoError = exception.GetType().Name;

        switch (exception)
        {
            case ValidacionException valEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = valEx.Message;
                break;

            case EntidadNoEncontradaException noEncEx:
                statusCode = HttpStatusCode.NotFound;
                mensaje = noEncEx.Message;
                break;

            case MigracionException migEx:
                statusCode = HttpStatusCode.UnprocessableEntity;
                mensaje = migEx.Message;
                break;

            case SigAppException sigEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = sigEx.Message;
                break;

            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = argEx.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                mensaje = "No tiene autorización para realizar esta acción.";
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var respuesta = new
        {
            exitoso = false,
            mensaje = mensaje,
            tipoError = tipoError,
            codigoEstado = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var opciones = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(respuesta, opciones));
    }
}
