using GestionDocumental.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionDocumental.Infrastructure.Data;

public sealed class Repositorio<T>(DocumentoDbContext context) : IRepositorio<T> 
    where T : class
{
    private readonly DocumentoDbContext _context = context;

    public async Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<T>().FindAsync([id], ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        // CRITICAL FIX: AsNoTracking() para operaciones de solo lectura
        return await _context.Set<T>()
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> BuscarAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicado, 
        CancellationToken ct = default)
    {
        return await _context.Set<T>()
            .AsNoTracking()
            .Where(predicado)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AgregarAsync(T entidad, CancellationToken ct = default)
    {
        await _context.Set<T>().AddAsync(entidad, ct).ConfigureAwait(false);
    }

    public Task ActualizarAsync(T entidad, CancellationToken ct = default)
    {
        _context.Set<T>().Update(entidad);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(T entidad, CancellationToken ct = default)
    {
        _context.Set<T>().Remove(entidad);
        return Task.CompletedTask;
    }
}