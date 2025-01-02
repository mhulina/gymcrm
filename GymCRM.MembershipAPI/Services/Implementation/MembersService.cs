using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Entities;
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

		public MemberDto GetByEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				throw new ArgumentException($"{email} is an invalid email address value");
			}

			try
			{
				var user = _repository.Members.FetchByCondition(x => x.Email == email).FirstOrDefault();
				var memberDto = _mapper.Map<MemberDto>(user);

				return memberDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				throw;
			}
		}

		public bool UpdateMember(MemberDto newMemberDto)
		{
			if (newMemberDto == null
			    || newMemberDto.Guid == Guid.Empty)
			{
				throw new ArgumentException($"{newMemberDto} is invalid");
			}

			try
			{
				var existingMember = _repository.Members
					.FetchByCondition(x => x.Guid == newMemberDto.Guid)
					.FirstOrDefault();

				if (existingMember is null)
				{
					var ex = new MemberNotFoundException("Member not found in DB");
					_logger.Error(ex, ex.Message);
					
					throw ex;
				}

				var newMember = _mapper.Map<Member>(newMemberDto);
				var updatedMember = MergeExistingMemberDataWithUpdateDate(newMember, existingMember);
				
				_repository.Members.Update(updatedMember);
				var result = _repository.Save();
				
				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				return false;
			}
		}

		public bool InsertMember(MemberDto newMemberDto)
		{
			newMemberDto.HashedPassword = new ASCIIEncoding().GetString(
				new MD5CryptoServiceProvider().ComputeHash(
					Encoding.ASCII.GetBytes(newMemberDto.HashedPassword)));

			try
			{
				var newMember = _mapper.Map<Member>(newMemberDto);
				
				_repository.Members.Insert(newMember);
				var result = _repository.Save();
				
				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				return false;
			}
		}

		private Member MergeExistingMemberDataWithUpdateDate(Member newMemberData, Member existingMemberData)
		{
			var updatedMember = new Member
			{
				Guid = newMemberData.Guid,
				Email = string.IsNullOrWhiteSpace(newMemberData.Email) 
					? existingMemberData.Email 
					: newMemberData.Email,
				DateJoined = DateTime.MinValue == newMemberData.DateJoined 
					? existingMemberData.DateJoined 
					: newMemberData.DateJoined,
				FirstName = string.IsNullOrWhiteSpace(newMemberData.FirstName) 
					? existingMemberData.FirstName 
					: newMemberData.FirstName,
				MiddleName = string.IsNullOrWhiteSpace(newMemberData.MiddleName)
					? existingMemberData.MiddleName
					: newMemberData.MiddleName,
				LastName = string.IsNullOrWhiteSpace(newMemberData.LastName)
					? existingMemberData.LastName
					: newMemberData.LastName,
				HashedPassword = string.IsNullOrWhiteSpace(newMemberData.HashedPassword)
					? existingMemberData.HashedPassword
					: newMemberData.HashedPassword,
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
				WorkingExperienceInMonths = newMemberData.WorkingExperienceInMonths,
				GymSubscriptionType = newMemberData.GymSubscriptionType,
				PersonalTrainerId = newMemberData.PersonalTrainerId,
				UserType = newMemberData.UserType,
				Gender = newMemberData.Gender
			};
			
			return updatedMember;
		}
	}
}
