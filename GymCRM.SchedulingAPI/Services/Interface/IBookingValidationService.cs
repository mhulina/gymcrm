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
}