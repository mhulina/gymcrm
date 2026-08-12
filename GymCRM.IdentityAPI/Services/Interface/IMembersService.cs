using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Exceptions;

namespace GymCRM.IdentityAPI.Services.Interface
{
	public interface IMembersService
	{
		/// <summary>
		/// Retrieves all users from the database asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing a list of <see cref="Member"/> objects representing all users.
		/// </returns>
		Task<List<Member>> GetAllUsersAsync(CancellationToken cancellationToken = default);
		/// <summary>
		/// Retrieves a user by their account GUID asynchronously.
		/// </summary>
		/// <param name="guid">The GUID of the user to retrieve.</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing a <see cref="Member"/> object if found; otherwise, null.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when the provided GUID is empty.</exception>
		Task<Member> GetUserByGuidAsync(Guid guid, CancellationToken cancellationToken = default);
		/// <summary>
		/// Retrieves a user by their email address asynchronously.
		/// </summary>
		/// <param name="email">The email address of the user to retrieve.</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing a <see cref="Member"/> object if found; otherwise, null.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when the provided email is null or whitespace.</exception>
		Task<Member> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
		/// <summary>
		/// Updates an existing member in the database with the provided member information asynchronously.
		/// </summary>
		/// <param name="insertMember">The member object containing updated information.</param>
		/// <param name="callerAccountGuid">The account making the request (from the JWT claim).</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing true if the update was successful; otherwise, false.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when the provided member is null or has an empty GUID.</exception>
		/// <exception cref="MemberNotFoundException">Thrown when the member does not exist in the database.</exception>
		/// <exception cref="MemberAccessDeniedException">Thrown when the caller may not update this member's profile.</exception>
		Task<bool> UpdateMemberAsync(Member insertMember, Guid callerAccountGuid, CancellationToken cancellationToken = default);
		/// <summary>
		/// Inserts a new member into the database asynchronously.
		/// </summary>
		/// <param name="insertMember">The member information to insert.</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing true if the insertion was successful; otherwise, false.
		/// </returns>
		Task<bool> InsertMemberAsync(InsertMember insertMember, CancellationToken cancellationToken = default);
		/// <summary>
		/// Uploads/replaces a member's profile photo, validating size and content type.
		/// </summary>
		/// <param name="accountGuid">The account whose photo is being set.</param>
		/// <param name="callerAccountGuid">The account making the request (from the JWT claim).</param>
		/// <param name="photoBytes">The raw photo bytes.</param>
		/// <param name="contentType">The photo's MIME type.</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <exception cref="ArgumentException">Thrown when no photo data was provided.</exception>
		/// <exception cref="PhotoTooLargeException">Thrown when the photo exceeds the allowed size.</exception>
		/// <exception cref="InvalidPhotoContentTypeException">Thrown when the content type is not an allowed image type.</exception>
		/// <exception cref="MemberNotFoundException">Thrown when the target member does not exist.</exception>
		/// <exception cref="MemberPhotoAccessDeniedException">Thrown when the caller may not change this member's photo.</exception>
		Task<bool> UploadMemberPhotoAsync(Guid accountGuid, Guid callerAccountGuid, byte[] photoBytes, string? contentType, CancellationToken cancellationToken = default);
		/// <summary>
		/// Retrieves a member's stored photo bytes and content type.
		/// </summary>
		/// <param name="accountGuid">The account whose photo is being fetched.</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>The photo bytes and content type, or null if the member has no photo set.</returns>
		/// <exception cref="MemberNotFoundException">Thrown when the member does not exist.</exception>
		Task<(byte[] Bytes, string ContentType)?> GetMemberPhotoAsync(Guid accountGuid, CancellationToken cancellationToken = default);
		/// <summary>
		/// Removes a member's stored profile photo.
		/// </summary>
		/// <param name="accountGuid">The account whose photo is being removed.</param>
		/// <param name="callerAccountGuid">The account making the request (from the JWT claim).</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <exception cref="MemberNotFoundException">Thrown when the target member does not exist.</exception>
		/// <exception cref="MemberPhotoAccessDeniedException">Thrown when the caller may not change this member's photo.</exception>
		Task<bool> DeleteMemberPhotoAsync(Guid accountGuid, Guid callerAccountGuid, CancellationToken cancellationToken = default);
	}
}
