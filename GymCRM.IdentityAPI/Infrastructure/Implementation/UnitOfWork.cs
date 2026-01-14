using GymCRM.IdentityAPI.Models.Interface;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.IdentityAPI.Infrastructure.Implementation;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var result = await _context.SaveChangesAsync(cancellationToken);
        
        return result > 0;
    }

    public void DetachAll()
    {
        foreach (var entityEntry in _context.ChangeTracker.Entries().ToList())
        {
            entityEntry.State = EntityState.Detached;
        }
    }

    public void Detach<T>(T entity) where T : class
    {
        var entry = _context.Entry(entity);
        
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
        
        var keyValue = entry.Property("Id").CurrentValue;
        var trackedEntry = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Property("Id").CurrentValue?.Equals(keyValue) == true 
                && e.State != EntityState.Detached);
    
        if (trackedEntry != null)
        {
            trackedEntry.State = EntityState.Detached;
        }
    }
}