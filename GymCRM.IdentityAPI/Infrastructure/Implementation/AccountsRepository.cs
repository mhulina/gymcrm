using System.Linq.Expressions;
using GymCRM.IdentityAPI.Models.Entities;
using GymCRM.IdentityAPI.Models.Interface;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.IdentityAPI.Models.Implementation;

public class AccountsRepository : IAccountsRepository
{
    private readonly AppDbContext _context;

    public AccountsRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Account>> FetchAllAccountsAsync(CancellationToken cancellationToken)
    {
        var result = await _context.Accounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<IEnumerable<Account>> FetchByConditionAsync(
        Expression<Func<Account, bool>> expression,
        CancellationToken cancellationToken)
    {
        var result = await _context.Accounts
            .Where(expression)
            .AsNoTracking()
            .Include(x => x.Member)
            .ToListAsync(cancellationToken);

        return result;
    }

    public void Insert(Account entity)
    {
        _context.Accounts.Add(entity);
    }

    public void Delete(Account entity)
    {
        _context.Accounts.Remove(entity);
    }

    public void Update(Account entity)
    {
        _context.Accounts.Update(entity);
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