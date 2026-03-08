namespace GestionDocumental.Application.Interfaces;

public interface IRepositorio<T> where T : class
{
    Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> BuscarAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicado, 
        CancellationToken cancellationToken = default);
    Task AgregarAsync(T entidad, CancellationToken cancellationToken = default);
    Task ActualizarAsync(T entidad, CancellationToken cancellationToken = default);
    Task EliminarAsync(T entidad, CancellationToken cancellationToken = default);
}