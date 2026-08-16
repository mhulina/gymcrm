using System.Linq.Expressions;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.BillingAPI.Infrastructure.Implementation;

public class SubscriptionsRepository : ISubscriptionsRepository
{
    private readonly BillingDbContext _context;

    public SubscriptionsRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Subscription>> FetchByConditionAsync(
        Expression<Func<Subscription, bool>> expression,
        CancellationToken cancellationToken)
    {
        var result = await _context.Subscriptions
            .Where(expression)
            .Include(x => x.Payments)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return result;
    }

    public void Insert(Subscription entity)
    {
        _context.Subscriptions.Add(entity);
    }

    public void Update(Subscription entity)
    {
        _context.Subscriptions.Update(entity);
    }

    private bool _disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        this._disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
