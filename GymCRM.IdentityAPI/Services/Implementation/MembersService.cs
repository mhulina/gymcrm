using AutoMapper;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.IdentityAPI.Services.Implementation
{
	public class MembersService : IMembersService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMembersRepository _repository;
		private readonly ILogger _logger;
		private readonly IMapper _mapper;

		public MembersService(
			IUnitOfWork unitOfWork,
			IMembersRepository repository,
			IMapper mapper,
			ILogger logger)
		{
			_unitOfWork = unitOfWork;
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<List<Member>> GetAllUsersAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				var dbUsers = await _repository.FetchAll(cancellationToken);
				var memberDtos = _mapper.Map<List<Member>>(dbUsers);

				return memberDtos;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public async Task<Member> GetUserByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
		{
			if (guid == Guid.Empty)
			{
				throw new ArgumentException($"{guid} is an invalid guid value");
			}

			try
			{
				var user = (await _repository
					.FetchByCondition(x => x.AccountGuid == guid, cancellationToken))
					.FirstOrDefault();
				var memberDto = _mapper.Map<Member>(user);

				return memberDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public async Task<Member> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				throw new ArgumentException($"{email} is an invalid email address value");
			}

			try
			{
				var user = (await _repository
					.FetchByCondition(x => x.Email == email, cancellationToken))
					.FirstOrDefault();
				var memberDto = _mapper.Map<Member>(user);

				return memberDto;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				throw;
			}
		}

		public async Task<bool> UpdateMemberAsync(Member insertMember, CancellationToken cancellationToken = default)
		{
			if (insertMember == null
			    || insertMember.AccountGuid == Guid.Empty)
			{
				throw new ArgumentException($"{insertMember} is invalid");
			}

			try
			{
				var existingMember = (await _repository
					.FetchByCondition(x => x.AccountGuid == insertMember.AccountGuid, cancellationToken))
					.FirstOrDefault();

				if (existingMember is null)
				{
					var ex = new MemberNotFoundException("Member not found in DB");
					_logger.Error(ex, ex.Message);
					
					throw ex;
				}

				var newMember = _mapper.Map<Models.Entities.Member>(insertMember);
				var updatedMember = MergeExistingMemberDataWithUpdateData(newMember, existingMember);
				
				_repository.Update(updatedMember);
				var result = await _unitOfWork.SaveAsync(cancellationToken);
				
				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				return false;
			}
		}

		public async Task<bool> InsertMemberAsync(InsertMember insertMember, CancellationToken cancellationToken = default)
		{
			try
			{
				var newMember = _mapper.Map<Models.Entities.Member>(insertMember);
				
				_repository.Insert(newMember);
				var result = await _unitOfWork.SaveAsync(cancellationToken);
				
				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				
				return false;
			}
		}

		/// <summary>
		/// Merges non-null and non-empty fields from the provided new member data into an existing member entity,
		/// returning a new <see cref="Models.Entities.Member"/> instance with updated data.
		/// </summary>
		/// <param name="newMemberData">The new member data containing updated values.</param>
		/// <param name="existingMemberData">The existing member data to merge into.</param>
		/// <returns>
		/// A new <see cref="Models.Entities.Member"/> instance with merged data.
		/// </returns>
		private Models.Entities.Member MergeExistingMemberDataWithUpdateData(
			Models.Entities.Member newMemberData, 
			Models.Entities.Member existingMemberData)
		{
			var updatedMember = new Models.Entities.Member
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
				Gender = newMemberData.Gender,
				DateModified = DateTime.UtcNow
			};
			
			return updatedMember;
		}
	}
}
