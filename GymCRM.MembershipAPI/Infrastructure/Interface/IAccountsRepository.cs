using System.Linq.Expressions;
using GymCRM.MembershipAPI.Infrastructure.Entities;

namespace GymCRM.MembershipAPI.Infrastructure.Interface;

public interface IAccountsRepository : IDisposable
{
    IEnumerable<Account> FetchAll();
    IEnumerable<Account> FetchByCondition(Expression<Func<Account, bool>> expression);
    void Insert(Account entity);
    bool Save();
    bool Delete(Account entity);
    void Update(Account entity);
}