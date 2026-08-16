using AutoMapper;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Enums;
using GymCRM.IdentityAPI.Models.Exceptions;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.IdentityAPI.Services.Implementation
{
	public class MembersService : IMembersService
	{
		private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;
		private static readonly HashSet<string> AllowedPhotoContentTypes =
			new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

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

		public async Task<bool> UpdateMemberAsync(Member insertMember, Guid callerAccountGuid, CancellationToken cancellationToken = default)
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

				await EnsureSelfOrAdminAsync(existingMember.AccountGuid, callerAccountGuid, cancellationToken);

				var newMember = _mapper.Map<Models.Entities.Member>(insertMember);
				var updatedMember = MergeExistingMemberDataWithUpdateData(newMember, existingMember);

				_repository.Update(updatedMember);
				var result = await _unitOfWork.SaveAsync(cancellationToken);

				return result;
			}
			catch (Exception ex) when (ex is not (MemberNotFoundException or MemberAccessDeniedException))
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

		public async Task<bool> UploadMemberPhotoAsync(
			Guid accountGuid,
			Guid callerAccountGuid,
			byte[] photoBytes,
			string? contentType,
			CancellationToken cancellationToken = default)
		{
			try
			{
				if (photoBytes is null || photoBytes.Length == 0)
				{
					throw new ArgumentException("No photo data was provided");
				}

				if (photoBytes.Length > MaxPhotoSizeBytes)
				{
					var ex = new PhotoTooLargeException(
						$"Photo exceeds the maximum allowed size of {MaxPhotoSizeBytes / (1024 * 1024)}MB");
					_logger.Warning(ex, "Rejected photo upload for {AccountGuid} - {SizeBytes} bytes exceeds the limit", accountGuid, photoBytes.Length);
					throw ex;
				}

				if (string.IsNullOrWhiteSpace(contentType) || !AllowedPhotoContentTypes.Contains(contentType))
				{
					var ex = new InvalidPhotoContentTypeException(
						$"Photo content type '{contentType}' is not supported. Allowed types: image/jpeg, image/png, image/webp");
					_logger.Warning(ex, "Rejected photo upload for {AccountGuid} - unsupported content type {ContentType}", accountGuid, contentType);
					throw ex;
				}

				var existingMember = await GetMemberForPhotoActionAsync(accountGuid, callerAccountGuid, cancellationToken);

				existingMember.Photo = photoBytes;
				existingMember.PhotoContentType = contentType;
				existingMember.DateModified = DateTime.UtcNow;

				_repository.Update(existingMember);
				var result = await _unitOfWork.SaveAsync(cancellationToken);

				_logger.Information("Photo uploaded for member {AccountGuid} by {CallerAccountGuid}", accountGuid, callerAccountGuid);

				return result;
			}
			catch (Exception ex) when (ex is not (PhotoTooLargeException or InvalidPhotoContentTypeException))
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public async Task<(byte[] Bytes, string ContentType)?> GetMemberPhotoAsync(
			Guid accountGuid,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var member = (await _repository
					.FetchByCondition(x => x.AccountGuid == accountGuid, cancellationToken))
					.FirstOrDefault();

				if (member is null)
				{
					var ex = new MemberNotFoundException("Member not found in DB");
					_logger.Error(ex, ex.Message);

					throw ex;
				}

				if (member.Photo is null || member.Photo.Length == 0 || string.IsNullOrWhiteSpace(member.PhotoContentType))
				{
					return null;
				}

				return (member.Photo, member.PhotoContentType);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		public async Task<bool> DeleteMemberPhotoAsync(
			Guid accountGuid,
			Guid callerAccountGuid,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var existingMember = await GetMemberForPhotoActionAsync(accountGuid, callerAccountGuid, cancellationToken);

				existingMember.Photo = null;
				existingMember.PhotoContentType = null;
				existingMember.DateModified = DateTime.UtcNow;

				_repository.Update(existingMember);
				var result = await _unitOfWork.SaveAsync(cancellationToken);

				_logger.Information("Photo deleted for member {AccountGuid} by {CallerAccountGuid}", accountGuid, callerAccountGuid);

				return result;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);

				throw;
			}
		}

		/// <summary>
		/// Fetches the target member for a photo upload/delete action, enforcing that the caller is
		/// either acting on their own account or is an Admin. Throws <see cref="MemberNotFoundException"/>
		/// or <see cref="MemberPhotoAccessDeniedException"/> as appropriate.
		/// </summary>
		private async Task<Models.Entities.Member> GetMemberForPhotoActionAsync(
			Guid accountGuid,
			Guid callerAccountGuid,
			CancellationToken cancellationToken)
		{
			var existingMember = (await _repository
				.FetchByCondition(x => x.AccountGuid == accountGuid, cancellationToken))
				.FirstOrDefault();

			if (existingMember is null)
			{
				var ex = new MemberNotFoundException("Member not found in DB");
				_logger.Error(ex, ex.Message);

				throw ex;
			}

			if (accountGuid != callerAccountGuid)
			{
				var caller = (await _repository
					.FetchByCondition(x => x.AccountGuid == callerAccountGuid, cancellationToken))
					.FirstOrDefault();

				if (caller is null || caller.AccountType != (int)AccountType.Admin)
				{
					var ex = new MemberPhotoAccessDeniedException();
					_logger.Warning(ex, "Blocked photo action on {AccountGuid} by non-owning, non-admin caller {CallerAccountGuid}", accountGuid, callerAccountGuid);
					throw ex;
				}
			}

			return existingMember;
		}

		/// <summary>
		/// Throws <see cref="MemberAccessDeniedException"/> unless the caller is either updating
		/// their own record or is an Admin.
		/// </summary>
		private async Task EnsureSelfOrAdminAsync(Guid targetAccountGuid, Guid callerAccountGuid, CancellationToken cancellationToken)
		{
			if (targetAccountGuid == callerAccountGuid)
			{
				return;
			}

			var caller = (await _repository
				.FetchByCondition(x => x.AccountGuid == callerAccountGuid, cancellationToken))
				.FirstOrDefault();

			if (caller is null || caller.AccountType != (int)AccountType.Admin)
			{
				var ex = new MemberAccessDeniedException();
				_logger.Warning(ex, "Blocked profile update on {AccountGuid} by non-owning, non-admin caller {CallerAccountGuid}", targetAccountGuid, callerAccountGuid);
				throw ex;
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
				Id = existingMemberData.Id,
				AccountGuid = newMemberData.AccountGuid,
				TimeZone = string.IsNullOrWhiteSpace(newMemberData.TimeZone)
					? existingMemberData.TimeZone
					: newMemberData.TimeZone,
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
				PersonalTrainerId = newMemberData.PersonalTrainerId,
				Gender = newMemberData.Gender,
				DateOfBirth = newMemberData.DateOfBirth ?? existingMemberData.DateOfBirth,
				HourlyPrice = newMemberData.HourlyPrice ?? existingMemberData.HourlyPrice,
				// Photo/PhotoContentType are never part of the update DTO - they're only ever
				// touched by the dedicated UploadPhoto/DeletePhoto endpoints. Without this explicit
				// carry-over, every profile save would silently wipe the member's photo, since the
				// mapped newMemberData always has Photo = null (see ConfigureIdentityMappings).
				Photo = existingMemberData.Photo,
				PhotoContentType = existingMemberData.PhotoContentType,
				DateModified = DateTime.UtcNow
			};
			
			return updatedMember;
		}
	}
}
