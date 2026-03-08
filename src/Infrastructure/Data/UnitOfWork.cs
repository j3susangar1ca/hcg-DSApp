using GestionDocumental.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GestionDocumental.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DocumentoDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(DocumentoDbContext context)
    {
        _context = context;
        Documentos = new Repositorio<Domain.Entities.DocumentoPrincipal>(context);
        Bitacoras = new Repositorio<Domain.Entities.BitacoraTrazabilidad>(context);
        Catalogos = new Repositorio<Domain.Entities.CatalogoCadido>(context);
    }

    public IRepositorio<Domain.Entities.DocumentoPrincipal> Documentos { get; }
    public IRepositorio<Domain.Entities.BitacoraTrazabilidad> Bitacoras { get; }
    public IRepositorio<Domain.Entities.CatalogoCadido> Catalogos { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }
    }

    public async Task CommitAsync()
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}