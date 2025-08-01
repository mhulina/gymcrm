using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.Interface;

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
}