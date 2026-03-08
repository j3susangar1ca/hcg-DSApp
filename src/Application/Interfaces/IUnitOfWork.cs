namespace GestionDocumental.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepositorio<Domain.Entities.DocumentoPrincipal> Documentos { get; }
    IRepositorio<Domain.Entities.BitacoraTrazabilidad> Bitacoras { get; }
    IRepositorio<Domain.Entities.CatalogoCadido> Catalogos { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}