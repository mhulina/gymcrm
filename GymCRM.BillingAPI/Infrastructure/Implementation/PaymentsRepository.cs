using System.Linq.Expressions;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.BillingAPI.Infrastructure.Implementation;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly BillingDbContext _context;

    public PaymentsRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payment>> FetchByConditionAsync(
        Expression<Func<Payment, bool>> expression,
        CancellationToken cancellationToken)
    {
        var result = await _context.Payments
            .Where(expression)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return result;
    }

    public void Insert(Payment entity)
    {
        _context.Payments.Add(entity);
    }

    public void Update(Payment entity)
    {
        _context.Payments.Update(entity);
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
