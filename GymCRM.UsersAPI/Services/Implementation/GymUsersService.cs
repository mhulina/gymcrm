using AutoMapper;
using GymCRM.UsersAPI.Infrastructure.Interface;
using GymCRM.UsersAPI.Models.DTOs;
using GymCRM.UsersAPI.Services.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.UsersAPI.Services.Implementation
{
	public class GymUsersService : IGymUsersService
	{
		private readonly IGymUsersRepository _repository;
		private readonly ILogger _logger;
		private readonly IMapper _mapper;

		public GymUsersService(
			IGymUsersRepository repository,
			IMapper mapper,
			ILogger logger)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public List<UserDto> GetAllUsers()
		{
			try
			{
				var dbUsers = _repository.GymUsers.FetchAll().ToList();
				var userDtos = _mapper.Map<List<UserDto>>(dbUsers);

				return userDtos;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public UserDto GetByGuid(Guid guid)
		{
			if (guid == Guid.Empty)
			{
				throw new ArgumentException($"{guid} is an invalid guid value");
			}

			try
			{
				var user = _repository.GymUsers.FetchByCondition(x => x.Guid == guid).FirstOrDefault();
				var userDto = _mapper.Map<UserDto>(user);

				return userDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}
	}
}
