using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface ITrainingSessionsService
{
    Task<IEnumerable<Models.DTOs.TrainingSession>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Models.DTOs.TrainingSession>> GetCancelledTrainingSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Models.DTOs.TrainingSession>> GetPendingTrainingSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Models.DTOs.TrainingSession>> GetCompletedTrainingSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Models.DTOs.TrainingSession>> GetTrainingSessionsForClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
    Task<bool> InsertTrainingSessionAsync(
        InsertTrainingSession insertTrainingSession,
        CancellationToken cancellationToken = default);
}