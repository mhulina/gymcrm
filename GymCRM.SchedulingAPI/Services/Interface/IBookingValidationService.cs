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
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    Task<ValidationResult> ValidateBookingAsync(
        InsertTrainingSession booking, 
        CancellationToken cancellationToken = default);
}