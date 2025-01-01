using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.MembershipAPI.Infrastructure.Implementation
{
	public class MembersRepository : IMembersRepository
	{
		private readonly AppDbContext _context;
		private readonly ILogger _logger;

		public IGenericRepository<Member> Members { get; private set; }

		public MembersRepository(AppDbContext context, ILogger logger)
		{
			_context = context;
			_logger = logger;
			Members = new GenericRepository<Member>(context);
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

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}
