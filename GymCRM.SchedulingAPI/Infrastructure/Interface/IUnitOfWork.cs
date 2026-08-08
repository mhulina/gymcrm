namespace GymCRM.SchedulingAPI.Infrastructure.Interface;

public interface IUnitOfWork
{
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}