using GymCRM.SchedulingAPI.Models.DTOs;
using TrainingSession = GymCRM.SchedulingAPI.Models.DTOs.TrainingSession;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface ITrainingSessionsService
{
    /// <summary>
    /// Retrieves all training sessions from the data store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of all training sessions.</returns>
    Task<IEnumerable<TrainingSession>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all training sessions that have been cancelled.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of cancelled training sessions.</returns>
    Task<IEnumerable<TrainingSession>> GetCancelledTrainingSessionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all training sessions that are currently pending.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of pending training sessions.</returns>
    Task<IEnumerable<TrainingSession>> GetPendingTrainingSessionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all training sessions that have been completed.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of completed training sessions.</returns>
    Task<IEnumerable<TrainingSession>> GetCompletedTrainingSessionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all training sessions associated with a specific client ID.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of training sessions for the specified client.</returns>
    Task<IEnumerable<TrainingSession>> GetTrainingSessionsForClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all training sessions associated with a specific trainer ID.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of training sessions for the specified client.</returns>
    Task<IEnumerable<TrainingSession>> GetTrainingSessionsForTrainerIdAsync(
        Guid trainerId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TrainingSession>> GetTrainingSessionsForTrainerIdInMonthAsync(
        Guid trainerId,
        int month,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Inserts a new training session into the data store.
    /// </summary>
    /// <param name="insertTrainingSession">The training session details to insert.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the session was successfully inserted; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="insertTrainingSession"/> is null.</exception>
    Task<bool> InsertTrainingSessionAsync(
        InsertTrainingSession insertTrainingSession,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes a training session by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the training session to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the session was successfully deleted; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
    Task<bool> DeleteTrainingSessionAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates an existing training session with the provided data.
    /// </summary>
    /// <param name="updatedTrainingSession">The updated training session DTO containing the new data.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is <c>true</c> if the update was successful; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="updatedTrainingSession"/> parameter is <c>null</c>.
    /// </exception>
    Task<bool> UpdateTrainingSessionAsync(TrainingSession updatedTrainingSession, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves a single training session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching training session, or <c>null</c> if not found.</returns>
    Task<TrainingSession?> GetTrainingSessionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Accepts a requested training session, promoting it to Booked.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if accepted; <c>false</c> if not found or not currently Requested.</returns>
    /// <exception cref="TrainingSessionAccessDeniedException">Thrown when the caller may not modify this session.</exception>
    Task<bool> AcceptTrainingSessionAsync(
        Guid id, Guid callerAccountGuid, bool callerIsAdmin, CancellationToken cancellationToken = default);
    /// <summary>
    /// Declines a requested training session, setting it to Cancelled.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if declined; <c>false</c> if not found or not currently Requested.</returns>
    /// <exception cref="TrainingSessionAccessDeniedException">Thrown when the caller may not modify this session.</exception>
    Task<bool> DeclineTrainingSessionAsync(
        Guid id, Guid callerAccountGuid, bool callerIsAdmin, CancellationToken cancellationToken = default);
    /// <summary>
    /// Reschedules a requested training session to a new time, promoting it to Booked. Assumes
    /// the caller has already validated the new time (e.g. via <c>IBookingValidationService</c>).
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="newStartTime">The new start time.</param>
    /// <param name="newEndTime">The new end time.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if rescheduled; <c>false</c> if not found or not currently Requested.</returns>
    /// <exception cref="TrainingSessionAccessDeniedException">Thrown when the caller may not modify this session.</exception>
    Task<bool> RescheduleTrainingSessionAsync(
        Guid id, DateTime newStartTime, DateTime newEndTime,
        Guid callerAccountGuid, bool callerIsAdmin, CancellationToken cancellationToken = default);
}