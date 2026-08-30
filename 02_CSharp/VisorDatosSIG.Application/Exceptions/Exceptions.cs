namespace VisorDatosSIG.Application.Exceptions;

public class SigAppException : Exception
{
    public SigAppException(string mensaje) : base(mensaje) { }
    public SigAppException(string mensaje, Exception inner) : base(mensaje, inner) { }
}

public class ValidacionException : SigAppException
{
    public List<string> Errores { get; }

    public ValidacionException(string mensaje) : base(mensaje)
    {
        Errores = new List<string> { mensaje };
    }

    public ValidacionException(List<string> errores) 
        : base("Se presentaron uno o más errores de validación: " + string.Join("; ", errores))
    {
        Errores = errores;
    }
}

public class MigracionException : SigAppException
{
    public string Capa { get; }
    public string? Archivo { get; }

    public MigracionException(string mensaje, string capa, string? archivo = null, Exception? inner = null) 
        : base(mensaje, inner!)
    {
        Capa = capa;
        Archivo = archivo;
    }
}

public class EntidadNoEncontradaException : SigAppException
{
    public EntidadNoEncontradaException(string entidad, object id) 
        : base($"No se encontró el registro de '{entidad}' con identificador '{id}'.") { }
}
