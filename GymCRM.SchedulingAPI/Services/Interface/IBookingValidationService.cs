using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface IBookingValidationService
{
    /// <summary>
    /// Validates a training session booking against business rules including time constraints, 
    /// trainer availability, holidays, time-off periods, and schedule conflicts.
    /// </summary>
    /// <param name="booking">The training session to validate.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <param name="excludeSessionId">
    /// A session Id to exclude from the overlap check - used when rescheduling an existing
    /// session, so it doesn't conflict with its own current (soon-to-be-replaced) time slot.
    /// </param>
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    Task<ValidationResult> ValidateBookingAsync(
        InsertTrainingSession booking,
        CancellationToken cancellationToken = default,
        Guid? excludeSessionId = null);
    /// <summary>
    /// Computes the bookable start times for a trainer on a given date, and the session
    /// durations (30/60/90 minutes) that fit at each one, taking the trainer's working hours,
    /// existing active sessions (with buffer), time off, and holidays into account.
    /// </summary>
    /// <param name="trainerId">The trainer to compute slots for.</param>
    /// <param name="date">The date to compute slots for, in the trainer's own local time.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The available slots for that date, sorted by start time. Empty if the trainer isn't working that day.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="trainerId"/> is empty.</exception>
    Task<List<AvailableSlot>> GetAvailableSlotsAsync(
        Guid trainerId,
        DateTime date,
        CancellationToken cancellationToken = default);
}