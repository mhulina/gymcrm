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

    Task<bool> AddWorkingHoursToDailyAvailability(
        Guid trainerId,
        string nameOfDay,
        List<InsertWorkingHours> newWorkingHours,
        CancellationToken cancellationToken = default);
}