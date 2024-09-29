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
		private IMapper _mapper;

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
			var dbUsers = _repository.GymUsers.FetchAll().ToList();
			var userDtos = _mapper.Map<List<UserDto>>(dbUsers);

			return userDtos;
		}
	}
}
