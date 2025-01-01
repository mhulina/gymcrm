using AutoMapper;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.MembershipAPI.Services.Implementation
{
	public class MembersService : IMembersService
	{
		private readonly IMembersRepository _repository;
		private readonly ILogger _logger;
		private readonly IMapper _mapper;

		public MembersService(
			IMembersRepository repository,
			IMapper mapper,
			ILogger logger)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public List<MemberDto> GetAllUsers()
		{
			try
			{
				var dbUsers = _repository.Members.FetchAll().ToList();
				var memberDtos = _mapper.Map<List<MemberDto>>(dbUsers);

				return memberDtos;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public MemberDto GetByGuid(Guid guid)
		{
			if (guid == Guid.Empty)
			{
				throw new ArgumentException($"{guid} is an invalid guid value");
			}

			try
			{
				var user = _repository.Members.FetchByCondition(x => x.Guid == guid).FirstOrDefault();
				var memberDto = _mapper.Map<MemberDto>(user);

				return memberDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}
	}
}
