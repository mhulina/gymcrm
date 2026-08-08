using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Enums;
using GymCRM.SchedulingAPI.Services.Interface;
using TrainingSession = GymCRM.SchedulingAPI.Models.Entities.TrainingSession;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class TrainingSessionsService : ITrainingSessionsService
{
    private readonly ITrainingSessionsRepository _trainingSessionsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TrainingSessionsService(
        ITrainingSessionsRepository trainingSessionsRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _trainingSessionsRepository = trainingSessionsRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchAllAsync(cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }

    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetCancelledTrainingSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchByConditionAsync(
            x => x.Status == (int)TrainingSessionStatus.Cancelled,
            cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }

    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetPendingTrainingSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchByConditionAsync(
            x => x.Status == (int)TrainingSessionStatus.Booked,
            cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }

    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetCompletedTrainingSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchByConditionAsync(
            x => x.Status == (int)TrainingSessionStatus.Completed,
            cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }

    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetTrainingSessionsForClientIdAsync(
        Guid clientId, 
        CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchByConditionAsync(
            x => x.ClientId == clientId,
            cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }
    
    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetTrainingSessionsForTrainerIdAsync(
        Guid trainerId, 
        CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchByConditionAsync(
            x => x.TrainerId == trainerId,
            cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }
    
    public async Task<IEnumerable<Models.DTOs.TrainingSession>> GetTrainingSessionsForTrainerIdInMonthAsync(
        Guid trainerId,
        int month,
        CancellationToken cancellationToken = default)
    {
        var result = await _trainingSessionsRepository.FetchByConditionAsync(
            x => x.TrainerId == trainerId
                && x.StartTime.Month == month,
            cancellationToken);
        var mappedResult = result
            .Select(x => _mapper.Map<Models.DTOs.TrainingSession>(x))
            .ToList();
        
        return mappedResult;
    }

    public async Task<bool> InsertTrainingSessionAsync(
        InsertTrainingSession insertTrainingSession,
        CancellationToken cancellationToken = default)
    {
        if (insertTrainingSession is null)
        {
            throw new ArgumentNullException(nameof(insertTrainingSession));
        }

        var trainingSessionEntity = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            TrainerId = insertTrainingSession.TrainerId,
            ClientId = insertTrainingSession.ClientId,
            Status = (int)TrainingSessionStatus.Requested,
            Description = insertTrainingSession.Description,
            StartTime = insertTrainingSession.StartTime,
            EndTime = insertTrainingSession.EndTime,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        };

        _trainingSessionsRepository.Add(trainingSessionEntity);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<bool> DeleteTrainingSessionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException($"{id} is an invalid value for ID", nameof(id));
        }

        var trainingSessionEntity = new TrainingSession
        {
            Id = id
        };
        
        _trainingSessionsRepository.Remove(trainingSessionEntity);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }

    public Task<bool> UpdateTrainingSessionAsync(
        Models.DTOs.TrainingSession updatedTrainingSession, 
        CancellationToken cancellationToken = default)
    {
        if (updatedTrainingSession is null)
        {
            throw new ArgumentNullException(nameof(updatedTrainingSession));
        }

        var mappedTrainingSession = _mapper.Map<TrainingSession>(updatedTrainingSession);
        mappedTrainingSession.DateModified = DateTime.UtcNow;

        _trainingSessionsRepository.Update(mappedTrainingSession);
        var result = _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Models.DTOs.TrainingSession?> GetTrainingSessionByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var entity = (await _trainingSessionsRepository
            .FetchByConditionAsync(x => x.Id == id, cancellationToken))
            .FirstOrDefault();

        return entity is null ? null : _mapper.Map<Models.DTOs.TrainingSession>(entity);
    }

    public async Task<bool> AcceptTrainingSessionAsync(
        Guid id, Guid callerAccountGuid, bool callerIsAdmin, CancellationToken cancellationToken = default)
    {
        var existingSession = (await _trainingSessionsRepository
            .FetchByConditionAsync(x => x.Id == id, cancellationToken))
            .FirstOrDefault();

        if (existingSession is null)
        {
            return false;
        }

        EnsureSelfOrAdmin(existingSession.TrainerId, callerAccountGuid, callerIsAdmin);

        if (existingSession.Status != (int)TrainingSessionStatus.Requested)
        {
            return false;
        }

        existingSession.Status = (int)TrainingSessionStatus.Booked;
        existingSession.DateModified = DateTime.UtcNow;

        _trainingSessionsRepository.Update(existingSession);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeclineTrainingSessionAsync(
        Guid id, Guid callerAccountGuid, bool callerIsAdmin, CancellationToken cancellationToken = default)
    {
        var existingSession = (await _trainingSessionsRepository
            .FetchByConditionAsync(x => x.Id == id, cancellationToken))
            .FirstOrDefault();

        if (existingSession is null)
        {
            return false;
        }

        EnsureSelfOrAdmin(existingSession.TrainerId, callerAccountGuid, callerIsAdmin);

        if (existingSession.Status != (int)TrainingSessionStatus.Requested)
        {
            return false;
        }

        existingSession.Status = (int)TrainingSessionStatus.Cancelled;
        existingSession.DateModified = DateTime.UtcNow;

        _trainingSessionsRepository.Update(existingSession);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RescheduleTrainingSessionAsync(
        Guid id, DateTime newStartTime, DateTime newEndTime,
        Guid callerAccountGuid, bool callerIsAdmin, CancellationToken cancellationToken = default)
    {
        var existingSession = (await _trainingSessionsRepository
            .FetchByConditionAsync(x => x.Id == id, cancellationToken))
            .FirstOrDefault();

        if (existingSession is null)
        {
            return false;
        }

        EnsureSelfOrAdmin(existingSession.TrainerId, callerAccountGuid, callerIsAdmin);

        if (existingSession.Status != (int)TrainingSessionStatus.Requested)
        {
            return false;
        }

        existingSession.StartTime = newStartTime;
        existingSession.EndTime = newEndTime;
        existingSession.Status = (int)TrainingSessionStatus.Booked;
        existingSession.DateModified = DateTime.UtcNow;

        _trainingSessionsRepository.Update(existingSession);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Throws <see cref="TrainingSessionAccessDeniedException"/> unless the caller is either an
    /// Admin or the trainer who owns the session being modified.
    /// </summary>
    private static void EnsureSelfOrAdmin(Guid ownerTrainerId, Guid callerAccountGuid, bool callerIsAdmin)
    {
        if (callerIsAdmin || ownerTrainerId == callerAccountGuid)
        {
            return;
        }

        throw new TrainingSessionAccessDeniedException();
    }
}