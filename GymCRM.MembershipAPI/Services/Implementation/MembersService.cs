using AutoMapper;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using ILogger = Serilog.ILogger;
using Member = GymCRM.MembershipAPI.Models.DTOs.Member;

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

		public List<Member> GetAllUsers()
		{
			try
			{
				var dbUsers = _repository.FetchAll().ToList();
				var memberDtos = _mapper.Map<List<Member>>(dbUsers);

				return memberDtos;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public Member GetByGuid(Guid guid)
		{
			if (guid == Guid.Empty)
			{
				throw new ArgumentException($"{guid} is an invalid guid value");
			}

			try
			{
				var user = _repository.FetchByCondition(x => x.AccountGuid == guid).FirstOrDefault();
				var memberDto = _mapper.Map<Member>(user);

				return memberDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public Member GetByEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				throw new ArgumentException($"{email} is an invalid email address value");
			}

			try
			{
				var user = _repository.FetchByCondition(x => x.Email == email).FirstOrDefault();
				var memberDto = _mapper.Map<Member>(user);

				return memberDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				throw;
			}
		}

		public bool UpdateMember(Member insertMember)
		{
			if (insertMember == null
			    || insertMember.AccountGuid == Guid.Empty)
			{
				throw new ArgumentException($"{insertMember} is invalid");
			}

			try
			{
				var existingMember = _repository
					.FetchByCondition(x => x.AccountGuid == insertMember.AccountGuid)
					.FirstOrDefault();

				if (existingMember is null)
				{
					var ex = new MemberNotFoundException("Member not found in DB");
					_logger.Error(ex, ex.Message);
					
					throw ex;
				}

				var newMember = _mapper.Map<Infrastructure.Entities.Member>(insertMember);
				var updatedMember = MergeExistingMemberDataWithUpdateDate(newMember, existingMember);
				
				_repository.Update(updatedMember);
				var result = _repository.Save();
				
				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				return false;
			}
		}

		public bool InsertMember(InsertMember insertMember)
		{
			try
			{
				var newMember = _mapper.Map<Infrastructure.Entities.Member>(insertMember);
				
				_repository.Insert(newMember);
				var result = _repository.Save();
				
				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				return false;
			}
		}

		private Infrastructure.Entities.Member MergeExistingMemberDataWithUpdateDate(Infrastructure.Entities.Member newMemberData, Infrastructure.Entities.Member existingMemberData)
		{
			var updatedMember = new Infrastructure.Entities.Member
			{
				AccountGuid = newMemberData.AccountGuid,
				Email = string.IsNullOrWhiteSpace(newMemberData.Email) 
					? existingMemberData.Email 
					: newMemberData.Email,
				FirstName = string.IsNullOrWhiteSpace(newMemberData.FirstName) 
					? existingMemberData.FirstName 
					: newMemberData.FirstName,
				MiddleName = string.IsNullOrWhiteSpace(newMemberData.MiddleName)
					? existingMemberData.MiddleName
					: newMemberData.MiddleName,
				LastName = string.IsNullOrWhiteSpace(newMemberData.LastName)
					? existingMemberData.LastName
					: newMemberData.LastName,
				MobileNumber = string.IsNullOrWhiteSpace(newMemberData.MobileNumber)
					? existingMemberData.MobileNumber
					: newMemberData.MobileNumber,
				PhoneNumber = string.IsNullOrWhiteSpace(newMemberData.PhoneNumber)
					? existingMemberData.PhoneNumber
					: newMemberData.PhoneNumber,
				WorkoutGroupIds = newMemberData.WorkoutGroupIds is null 
				    || !newMemberData.WorkoutGroupIds.Any()
						? existingMemberData.WorkoutGroupIds
						: newMemberData.WorkoutGroupIds,
				AccountType = newMemberData.AccountType == existingMemberData.AccountType
					? existingMemberData.AccountType
					: newMemberData.AccountType,
				WorkingExperienceInMonths = newMemberData.WorkingExperienceInMonths,
				GymSubscriptionType = newMemberData.GymSubscriptionType,
				PersonalTrainerId = newMemberData.PersonalTrainerId,
				Gender = newMemberData.Gender
			};
			
			return updatedMember;
		}
	}
}
