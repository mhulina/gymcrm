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
}