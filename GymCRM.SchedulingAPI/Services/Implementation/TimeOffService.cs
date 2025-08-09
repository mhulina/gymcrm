using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class TimeOffService : ITimeOffService
{
    private readonly ITimeOffRepository _timeOffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TimeOffService(
        ITimeOffRepository timeOffRepository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper)
    {
        _timeOffRepository = timeOffRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<TimeOff>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _timeOffRepository.FetchAllAsync(cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<IEnumerable<TimeOff>>(result);
        
        return mappedResult;
    }

    public async Task<IEnumerable<TimeOff>> GetAllForTrainerIdAsync(
        Guid trainerId, 
        CancellationToken cancellationToken = default)
    {
        if (trainerId == Guid.Empty)
        {
            throw new ArgumentException($"{trainerId} is an invalid trainer ID", nameof(trainerId));
        }
        
        var result = await _timeOffRepository.FetchByConditionAsync(
            x => x.TrainerId == trainerId, 
            cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<IEnumerable<TimeOff>>(result);
        
        return mappedResult;
    }

    public async Task<IDictionary<DateTime, List<TimeOff>>> GetAllForDatePeriodAsync(
        DateTime startDate, 
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException($"{endDate} is less than {startDate}");
        }

        if (startDate == DateTime.MinValue
            || startDate == DateTime.MaxValue
            || endDate == DateTime.MinValue)
        {
            throw new InvalidOperationException($"Date values cannot be Max or Min values of DateTime");
        }
        
        var result = (await _timeOffRepository
            .FetchByConditionAsync(
                x => x.Date <= DateTime.SpecifyKind(endDate, DateTimeKind.Utc)
                    && x.Date >= DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
                cancellationToken: cancellationToken))
            .GroupBy(x => x.Date)
            .ToDictionary(k => k.Key, v => v.Select(x => x).ToList());
        var mappedResult = _mapper.Map<IDictionary<DateTime, List<TimeOff>>>(result);

        return mappedResult;
    }

    public async Task<bool> AddTimeOffAsync(InsertTimeOff insertTimeOff, CancellationToken cancellationToken = default)
    {
        if (insertTimeOff is null)
        {
            throw new ArgumentNullException(nameof(insertTimeOff));
        }

        var timeOff = new Models.Entities.TimeOff()
        {
            Id = Guid.CreateVersion7(),
            TrainerId = insertTimeOff.TrainerId,
            Date = insertTimeOff.Date,
            Reason = insertTimeOff.Reason,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        };
        
        _timeOffRepository.Add(timeOff);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }

    public async Task<bool> DeleteTimeOffAsync(Guid timeOffId, CancellationToken cancellationToken = default)
    {
        if (timeOffId == Guid.Empty)
        {
            throw new ArgumentException($"{timeOffId} is an invalid TimeOff ID", nameof(timeOffId));
        }

        var timeOff = new Models.Entities.TimeOff
        {
            Id = timeOffId
        };
        
        _timeOffRepository.Remove(timeOff);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<bool> UpdateTimeOffAsync(TimeOff updatedTimeOff, CancellationToken cancellationToken = default)
    {
        if (updatedTimeOff is null)
        {
            throw new ArgumentNullException(nameof(updatedTimeOff));
        }
        
        var timeOff = _mapper.Map<Models.Entities.TimeOff>(updatedTimeOff);
        timeOff.DateModified = DateTime.UtcNow;
        
        _timeOffRepository.Update(timeOff);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }
}