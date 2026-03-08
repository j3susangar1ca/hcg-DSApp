using GestionDocumental.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionDocumental.Infrastructure.Data;

public sealed class Repositorio<T> : IRepositorio<T> where T : class
{
    private readonly DocumentoDbContext _context;

    public Repositorio(DocumentoDbContext context)
    {
        _context = context;
    }

    public async Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<T>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> BuscarAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicado, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().Where(predicado).ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(T entidad, CancellationToken cancellationToken = default)
    {
        await _context.Set<T>().AddAsync(entidad, cancellationToken);
    }

    public Task ActualizarAsync(T entidad, CancellationToken cancellationToken = default)
    {
        _ = _context.Set<T>().Update(entidad);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(T entidad, CancellationToken cancellationToken = default)
    {
        _ = _context.Set<T>().Remove(entidad);
        return Task.CompletedTask;
    }
}