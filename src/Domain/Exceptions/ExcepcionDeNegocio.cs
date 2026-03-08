namespace GestionDocumental.Domain.Exceptions;

public sealed class ExcepcionDeNegocio : Exception
{
    public ExcepcionDeNegocio(string mensaje) : base(mensaje) { }
    
    public ExcepcionDeNegocio(string mensaje, Exception innerException) 
        : base(mensaje, innerException) { }
}