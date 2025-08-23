using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface IAvailabilitiesService
{
    /// <summary>
    /// Retrieves all availability records.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a collection of <see cref="Availability"/> objects.</returns>
    Task<IEnumerable<Availability>> GetAvailabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves all availability records for a specific trainer.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a collection of <see cref="Availability"/> objects for the given trainer.</returns>
    Task<IEnumerable<Availability>> GetAvailabilitiesForTrainerIdAsync(Guid id, CancellationToken cancellationToken = default);
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
    /// <param name="availability">The updated <see cref="Availability"/> object.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="availability"/> is null.</exception>
    Task<bool> UpdateAvailabilityAsync(Availability availability, CancellationToken cancellationToken = default);
}