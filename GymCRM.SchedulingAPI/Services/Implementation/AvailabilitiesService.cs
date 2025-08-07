using AutoMapper;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class AvailabilitiesService : IAvailabilitiesService
{
    private readonly IAvailabilitiesRepository _availabilitiesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AvailabilitiesService(
        IAvailabilitiesRepository availabilitiesRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _availabilitiesRepository = availabilitiesRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<Availability>> GetAvailabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _availabilitiesRepository.FetchAllAsync(cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<IEnumerable<Availability>>(result);
        
        return mappedResult;
    }

    public async Task<IEnumerable<Availability>> GetAvailabilitiesForTrainerIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var result = await _availabilitiesRepository.FetchByConditionAsync(
            x => x.TrainerId == id,
            cancellationToken: cancellationToken);
        var mappedResult = _mapper.Map<IEnumerable<Availability>>(result);
        
        return mappedResult;
    }

    public async Task<bool> AddAvailabilityAsync(
        InsertAvailability insertAvailability,
        CancellationToken cancellationToken = default)
    {
        if (insertAvailability is null)
        {
            throw new ArgumentNullException(nameof(insertAvailability));
        }

        var availability = new Models.Entities.Availability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = insertAvailability.TrainerId,
            DayOfWeek = insertAvailability.DayOfWeek,
            StartDate = insertAvailability.StartDate,
            EndDate = insertAvailability.EndDate,
            IsAvailable = insertAvailability.IsAvailable,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };
        
        _availabilitiesRepository.Add(availability);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<bool> DeleteAvailabilityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(nameof(id));
        }

        var availability = new Models.Entities.Availability
        {
            Id = id
        };
        
        _availabilitiesRepository.Remove(availability);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }

    public async Task<bool> UpdateAvailabilityAsync(
        Availability availability,
        CancellationToken cancellationToken = default)
    {
        if (availability is null)
        {
            throw new ArgumentNullException(nameof(availability));
        }
        
        var mappedAvailability = _mapper.Map<Models.Entities.Availability>(availability);
        
        _availabilitiesRepository.Update(mappedAvailability);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return result;
    }
}