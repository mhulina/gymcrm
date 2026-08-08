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
    /// Returns the TrainerIds of every trainer with at least one bookable working-hours range
    /// (a working-hours entry on a day that isn't marked as a day off). Used to filter out
    /// trainers with no configured hours from the booking flow.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of trainer GUIDs.</returns>
    Task<List<Guid>> GetTrainerIdsWithWorkingHoursAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a new availability record.
    /// </summary>
    /// <param name="insertAvailability">The DTO containing the details of the availability to add.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="insertAvailability"/> is null.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> AddAvailabilityAsync(
        InsertAvailability insertAvailability,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes an existing availability record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the availability to delete.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully deleted.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> DeleteAvailabilityAsync(
        Guid id,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates an existing availability record.
    /// </summary>
    /// <param name="trainerAvailability">The updated <see cref="TrainerAvailability"/> object.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the availability was successfully updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="trainerAvailability"/> is null.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> UpdateAvailabilityAsync(
        TrainerAvailability trainerAvailability,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds working hours to a trainer's availability for a specific day of the week.
    /// Creates the daily availability if it doesn't exist.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="nameOfDay">The name of the day of the week (e.g., "Monday").</param>
    /// <param name="newWorkingHours">The list of working hour periods to add.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>
    /// <c>true</c> if the working hours were successfully added;
    /// <c>false</c> if the trainer has no availability record.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="trainerId"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="nameOfDay"/> is not a valid day of the week.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> AddWorkingHoursToDailyAvailability(
        Guid trainerId,
        string nameOfDay,
        List<InsertWorkingHours> newWorkingHours,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates the start/end time of an existing working-hours range.
    /// </summary>
    /// <param name="id">The unique identifier of the working-hours range to update.</param>
    /// <param name="updatedWorkingHours">The new start/end time.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the working hours were successfully updated; <c>false</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="updatedWorkingHours"/> is null.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> UpdateWorkingHoursAsync(
        Guid id,
        InsertWorkingHours updatedWorkingHours,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes an existing working-hours range.
    /// </summary>
    /// <param name="id">The unique identifier of the working-hours range to delete.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the working hours were successfully deleted; <c>false</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> DeleteWorkingHoursAsync(
        Guid id,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Sets whether a given day of the week is a day off for a trainer, creating the daily
    /// availability record if it doesn't exist yet. Toggling an existing day to a day off
    /// removes any working-hours ranges configured for that day.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="nameOfDay">The name of the day of the week (e.g., "Monday").</param>
    /// <param name="isDayOff">Whether the day should be marked as a day off.</param>
    /// <param name="callerAccountGuid">The account GUID of the caller, from the JWT.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin, from the JWT.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the day-off status was successfully applied.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="trainerId"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="nameOfDay"/> is not a valid day of the week.</exception>
    /// <exception cref="TrainerAvailabilityAccessDeniedException">Thrown when the caller may not modify this trainer's availability.</exception>
    Task<bool> SetDayOffStatusAsync(
        Guid trainerId,
        string nameOfDay,
        bool isDayOff,
        Guid callerAccountGuid,
        bool callerIsAdmin,
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
