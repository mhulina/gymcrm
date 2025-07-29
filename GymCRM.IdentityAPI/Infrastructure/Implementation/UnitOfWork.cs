using GymCRM.IdentityAPI.Models.Interface;

namespace GymCRM.IdentityAPI.Models.Implementation;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var result = await _context.SaveChangesAsync(cancellationToken);
        
        return result > 0;
    }
}