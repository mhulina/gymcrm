using System.Linq.Expressions;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace GymCRM.MembershipAPI.Infrastructure.Implementation;

public class AccountsRepository : IAccountsRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger _logger;

    public AccountsRepository(AppDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public IEnumerable<Account> FetchAll()
    {
        var result = _context.Accounts.AsNoTracking();

        return result;
    }

    public IEnumerable<Account> FetchByCondition(Expression<Func<Account, bool>> expression)
    {
        var result = _context.Accounts.Where(expression).AsNoTracking();

        return result;
    }

    public void Insert(Account entity)
    {
        _context.Accounts.Add(entity);
    }

    public bool Save()
    {
        try
        {
            _context.Database.BeginTransaction();
            var result = _context.SaveChanges();
            _context.Database.CommitTransaction();

            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            _context.Database.RollbackTransaction();
            throw;
        }
    }

    public bool Delete(Account entity)
    {
        if (entity.Guid == Guid.Empty)
        {
            return false;
        }
        
        var result = _context.Accounts
            .AsNoTracking()
            .FirstOrDefault(x => x.Guid == entity.Guid);

        if (result is null)
        {
            return false;
        }
        
        _context.Accounts.Remove(result);

        return true;
    }

    public void Update(Account entity)
    {
        throw new NotImplementedException();
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