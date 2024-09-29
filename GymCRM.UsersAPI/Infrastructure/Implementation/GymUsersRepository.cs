using GymCRM.UsersAPI.Infrastructure.Entities;
using GymCRM.UsersAPI.Infrastructure.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.UsersAPI.Infrastructure.Implementation
{
	public class GymUsersRepository : IGymUsersRepository
	{
		private readonly AppDbContext _context;
		private readonly ILogger _logger;

		public IGenericRepository<User> GymUsers { get; private set; }

		public GymUsersRepository(AppDbContext context, ILogger logger)
		{
			_context = context;
			_logger = logger;
			GymUsers = new GenericRepository<User>(context);
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
