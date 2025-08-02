using GymCRM.SchedulingAPI.Infrastructure.Interface;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class UnitOfWork : IUnitOfWork
{
    private readonly SchedulingDbContext _context;
    
    public UnitOfWork(SchedulingDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var result = await _context.SaveChangesAsync(cancellationToken);
        
        return result > 0;
    }
}