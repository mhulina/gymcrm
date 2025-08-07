using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface IAvailabilitiesService
{
    Task<IEnumerable<Availability>> GetAvailabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Availability>> GetAvailabilitiesForTrainerIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AddAvailabilityAsync(InsertAvailability insertAvailability, CancellationToken cancellationToken = default);
    Task<bool> DeleteAvailabilityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateAvailabilityAsync(Availability availability, CancellationToken cancellationToken = default);
}