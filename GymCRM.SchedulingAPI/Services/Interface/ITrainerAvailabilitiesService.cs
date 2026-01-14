using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface ITrainerAvailabilitiesService
{
    /// <summary>
    /// Retrieves all availability records.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a collection of <see cref="TrainerAvailability"/> objects.</returns>
    Task<IEnumerable<TrainerAvailability>> GetAvailabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all availability records for a specific trainer.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a collection of <see cref="TrainerAvailability"/> objects for the given trainer.</returns>
    Task<IEnumerable<TrainerAvailability>> GetAvailabilitiesForTrainerIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a new availability record.
    /// </summary>
    /// <param name="insertAvailability">The DTO containing the details of the availability to add.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="insertAvailability"/> is null.</exception>
    Task<bool> AddAvailabilityAsync(InsertAvailability insertAvailability, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes an existing availability record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the availability to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully deleted.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
    Task<bool> DeleteAvailabilityAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates an existing availability record.
    /// </summary>
    /// <param name="trainerAvailability">The updated <see cref="TrainerAvailability"/> object.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="trainerAvailability"/> is null.</exception>
    Task<bool> UpdateAvailabilityAsync(TrainerAvailability trainerAvailability, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds working hours to a trainer's availability for a specific day of the week.
    /// Creates the daily availability if it doesn't exist.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="nameOfDay">The name of the day of the week (e.g., "Monday").</param>
    /// <param name="newWorkingHours">The list of working hour periods to add.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>
    /// <c>true</c> if the working hours were successfully added; 
    /// <c>false</c> if the trainer has no availability record.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="trainerId"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="nameOfDay"/> is not a valid day of the week.</exception>
    Task<bool> AddWorkingHoursToDailyAvailability(
        Guid trainerId,
        string nameOfDay,
        List<InsertWorkingHours> newWorkingHours,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Determines whether a trainer is available to work during the specified time period 
    /// based on their configured availability and working hours.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="startTime">The start of the requested time period.</param>
    /// <param name="endTime">The end of the requested time period.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>
    /// <c>true</c> if the trainer is scheduled to work and available during the entire requested period; 
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="trainerId"/> is empty or when <paramref name="startTime"/> 
    /// or <paramref name="endTime"/> are invalid (MinValue or MaxValue).
    /// </exception>
    Task<bool> IsTrainerWorkingOnDateAsync(
        Guid trainerId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}