using GymCRM.MembershipAPI.Infrastructure.Interface;

namespace GymCRM.MembershipAPI.Infrastructure.Implementation;

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